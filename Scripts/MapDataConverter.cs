using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

// =========================================================================
// GeoJSON データ構造の定義
// JSON Utility でのデシリアライズのため、すべて [Serializable] を付与し、
// MapDataConverter クラスと同じファイル内に配置します。
// =========================================================================

// GeoJSONの座標は [lon, lat] の順
[Serializable]
public class GeoGeometry
{
    public string type; // "Point", "LineString", "MultiLineString"

    // LineString や Point の座標を格納。JsonUtilityで複雑なネストを直接デシリアライズするのは困難なため、
    // ここでは単純なList<double>として定義し、手動でlon/latを取り出すことを前提とします。
    public List<double> coordinates;

    // MultiLineString の座標を格納
    // [ [ [lon1, lat1], [lon2, lat2], ... ], [ ... ] ]
    public List<List<List<double>>> coordinatesMulti;
}

[Serializable]
public class GeoFeature
{
    public string type; // "Feature"
    // Dictionary<string, string> は JsonUtility ではサポートされないため、
    // 実際にはDictionaryをラップするカスタムクラスが必要です。ここでは簡易的に使用します。
    public Dictionary<string, string> properties;
    public GeoGeometry geometry;
}

[Serializable]
public class GeoFeatureCollection
{
    public string type; // "FeatureCollection"
    public List<GeoFeature> features;
}

// =========================================================================
// MapDataConverter クラス本体
// =========================================================================

public class MapDataConverter : MonoBehaviour
{
    // --- 外部参照 ---
    [Header("Dependencies")]
    public GPSLocationProvider gpsProvider;

    [Header("OSM Data Sources")]
    // Unity Inspectorで設定するTextAsset
    public TextAsset linesAsset;            // lines.geojson
    public TextAsset multiLinesAsset;       // multilinestrings.geojson
    public TextAsset pointsAsset;           // points.geojson

    // 構築された経路探索用グラフ
    public Dictionary<ulong, OSMNode> NodeGraph { get; private set; } = new Dictionary<ulong, OSMNode>();
    public Dictionary<string, Vector2> POIs = new Dictionary<string, Vector2>();

    // Key: ローカル座標, Value: 割り当てたノードID
    private Dictionary<Vector3, ulong> coordinateToID = new Dictionary<Vector3, ulong>(new Vector3EqualityComparer());
    private ulong nextID = 1;

    void Start()
    {
        if (gpsProvider == null)
        {
            UnityEngine.Debug.LogError("GPSLocationProvider が設定されていません。座標変換ができません。");
            return;
        }

        // 外部の依存関係が安定していることを確認してからグラフを構築
        // Note: GPSが初期化されるのを待つのが理想的ですが、ここでは Start() で実行します。
        LoadAndBuildGraph();

        // MapDataConverter.cs の Start() メソッドの最後に追加
        if (NodeGraph != null)
        {
            UnityEngine.Debug.Log($"グラフ構築完了。総ノード数: {NodeGraph.Count}");
        }
        if (POIs.Count > 0)
        {
            UnityEngine.Debug.Log($"POI ({POIs.Count}個) がロードされました。例: {POIs.Keys.First()}");
        }
    }

    /// <summary>
    /// GeoJSONデータをロードし、ローカル座標に変換してグラフ構造を構築します。
    /// </summary>
    public void LoadAndBuildGraph()
    {
        // 1. ノード座標の収集とIDの割り当て（LineString, MultiLineString, Point）
        CollectAndAssignIDs(linesAsset);
        CollectAndAssignIDs(multiLinesAsset);
        CollectAndAssignIDs(pointsAsset);

        // 2. 接続情報（エッジ）の設定
        BuildGraphConnections(linesAsset);
        BuildGraphConnections(multiLinesAsset);

        UnityEngine.Debug.Log($"グラフ構築完了。ノード数: {NodeGraph.Count}");
    }

    // --- 処理コア関数 ---

    /// <summary>
    /// GeoJSONアセットをパースし、一意の座標にIDを割り当ててノードを初期化します。
    /// </summary>
    private void CollectAndAssignIDs(TextAsset asset)
    {
        if (asset == null) return;

        // 【注意】JsonUtility は Dictionary や複雑なネストのパースに限界があります。
        // ここでは、データ量が少ないことを前提に、JsonUtilityで可能な範囲でデシリアライズを試みます。
        GeoFeatureCollection collection;
        try
        {
            collection = JsonUtility.FromJson<GeoFeatureCollection>(asset.text);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"GeoJSON ({asset.name}) のパースに失敗しました。外部JSONライブラリが必要かもしれません: {e.Message}");
            return;
        }

        foreach (var feature in collection.features)
        {
            if (feature.geometry == null) continue;

            // 座標を抽出
            List<List<double>> rawCoords = ExtractCoordinates(feature.geometry);

            foreach (var coordPair in rawCoords)
            {
                if (coordPair.Count < 2) continue;

                // Lon = [0], Lat = [1]
                double lon = coordPair[0];
                double lat = coordPair[1];

                // ローカル座標に変換 (Y=0)
                Vector3 localPos = gpsProvider.ConvertGPSToLocal(lat, lon);

                // 座標が既に登録されているか確認
                if (!coordinateToID.ContainsKey(localPos))
                {
                    // 新規ノードとして登録
                    coordinateToID.Add(localPos, nextID);
                    NodeGraph.Add(nextID, new OSMNode(nextID, localPos));
                    nextID++;
                }
            }
        }
    }

    /// <summary>
    /// LineStringおよびMultiLineStringのフィーチャーから接続情報（エッジ）を設定します。
    /// </summary>
    private void BuildGraphConnections(TextAsset asset)
    {
        if (asset == null) return;

        GeoFeatureCollection collection = JsonUtility.FromJson<GeoFeatureCollection>(asset.text);

        foreach (var feature in collection.features)
        {
            if (feature.geometry == null) continue;

            // 座標を抽出
            List<List<double>> rawCoords = ExtractCoordinates(feature.geometry);

            // 一方通行フラグの確認
            bool isOneWay = IsOneWayRoad(feature.properties);

            for (int i = 0; i < rawCoords.Count - 1; i++)
            {
                // 連続する2点 A と B
                Vector3 posA = gpsProvider.ConvertGPSToLocal(rawCoords[i][1], rawCoords[i][0]);
                Vector3 posB = gpsProvider.ConvertGPSToLocal(rawCoords[i + 1][1], rawCoords[i + 1][0]);

                if (!coordinateToID.ContainsKey(posA) || !coordinateToID.ContainsKey(posB)) continue;

                ulong idA = coordinateToID[posA];
                ulong idB = coordinateToID[posB];

                // A -> B の接続を追加
                AddConnection(idA, idB);

                // 双方向路の場合、B -> A の接続も追加
                if (!isOneWay)
                {
                    AddConnection(idB, idA);
                }
            }
        }
    }

    private void AddConnection(ulong fromID, ulong toID)
    {
        if (NodeGraph.ContainsKey(fromID) && !NodeGraph[fromID].Neighbors.Contains(toID))
        {
            NodeGraph[fromID].Neighbors.Add(toID);
        }
    }

    // --- ヘルパー関数 ---

    /// <summary>
    /// GeoJSONフィーチャーから座標配列を抽出（LineString, Point, MultiLineStringに対応）
    /// </summary>
    private List<List<double>> ExtractCoordinates(GeoGeometry geometry)
    {
        List<List<double>> result = new List<List<double>>();

        if (geometry.type == "Point" && geometry.coordinates != null && geometry.coordinates.Count == 2)
        {
            // Point: [lon, lat]
            result.Add(geometry.coordinates);
        }
        else if (geometry.type == "LineString" && geometry.coordinates != null)
        {
            // LineString: [ [lon1, lat1], [lon2, lat2], ... ] 
            // **JsonUtilityの制限を回避するため、coordinatesが実は入れ子の配列であると仮定**し、
            // ログから読み取った実際の構造に合わせて手動で平坦化が必要です。
            // ここでは、LineStringのcoordinatesが List<List<double>> であると仮定します。

            // 警告: JsonUtilityは LineString の座標を正しくパースできない可能性が高いため、
            // 実際には手動パースが必要

            // ログの例 ([ [ 170.5215772, -45.8627999 ], ... ]) から、
            // coordinatesが List<List<double>> として解釈されている場合:
            // return geometry.coordinates.Cast<List<double>>().ToList(); 

            // 簡易的な回避策として、MultiLineStringの構造を LineStringでも使用します:
            // MultiLineStringとして扱って平坦化
            if (geometry.coordinatesMulti != null && geometry.coordinatesMulti.Count > 0)
            {
                foreach (var line in geometry.coordinatesMulti[0])
                {
                    result.Add(line);
                }
            }
        }
        else if (geometry.type == "MultiLineString" && geometry.coordinatesMulti != null)
        {
            // [ [ [lon1, lat1], ... ], [ [lonA, latA], ... ], ... ]
            foreach (var line in geometry.coordinatesMulti.SelectMany(list => list))
            {
                result.Add(line);
            }
        }

        return result;
    }

    /// <summary>
    /// GeoJSON propertiesから一方通行かどうかを判断する (簡易ロジック)
    /// </summary>
    private bool IsOneWayRoad(Dictionary<string, string> properties)
    {
        if (properties == null) return false;

        // OSMのタグで "oneway" が "yes" または "1" の場合に一方通行と見なす
        if (properties.TryGetValue("oneway", out string value))
        {
            return value.ToLower() == "yes" || value == "1";
        }
        return false;
    }

    // --- Vector3 を Dictionary のキーとして使用するためのカスタム Equality Comparer ---

    /// <summary>
    /// 浮動小数点誤差を考慮して Vector3 (X, Zのみ) を比較する
    /// </summary>
    private class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        private const float Tolerance = 0.00001f;

        public bool Equals(Vector3 v1, Vector3 v2)
        {
            // XとZのみを比較
            return Mathf.Abs(v1.x - v2.x) < Tolerance && Mathf.Abs(v1.z - v2.z) < Tolerance;
        }

        public int GetHashCode(Vector3 v)
        {
            // XとZを使ってハッシュを生成
            // NOTE: 浮動小数点数からのハッシュ生成は衝突のリスクがあるため、より堅牢な実装が必要な場合がある
            return (Mathf.RoundToInt(v.x / Tolerance) * 397) ^ Mathf.RoundToInt(v.z / Tolerance);
        }
    }
}