using UnityEngine;
using System.Collections;

public class GPSLocationProvider : MonoBehaviour
{
    // --- 外部設定 ---
    [Header("Coordinate System Settings")]
    [Tooltip("シーンの原点 (0, 0, 0) に対応する緯度。")]
    public double ReferenceLatitude = -45.86438;
    [Tooltip("シーンの原点 (0, 0, 0) に対応する経度。")]
    public double ReferenceLongitude = 170.51731;
    [Tooltip("1メートルあたりの緯度/経度の変換係数（ Dunedin, NZ の緯度で約 111139.0 / 76600.0 が目安）")]
    public float MetersPerDegreeLon = 76600.0f; // 経度方向のスケール係数 (m/度)
    public float MetersPerDegreeLat = 111139.0f; // 緯度方向のスケール係数 (m/度)

    // --- 公開プロパティ (現在の位置) ---
    public Vector3 CurrentLocalPosition { get; private set; }
    public double CurrentLatitude { get; private set; }
    public double CurrentLongitude { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    // --- 初期化 ---
    void Start()
    {
        // GPSの取得を開始するコルーチンを呼び出す
        StartCoroutine(StartLocationService());
    }

    /// <summary>
    /// GPSサービスを初期化し、位置情報の更新を開始します。
    /// </summary>
    IEnumerator StartLocationService()
    {
        // 1. 位置情報サービスが有効か確認
        if (!Input.location.isEnabledByUser)
        {
            UnityEngine.Debug.LogError("GPSアクセスが無効です。デバイスの設定を確認してください。");
            yield break;
        }

        // 2. サービスを開始
        Input.location.Start();

        // 3. 初期化を待機 (最大20秒)
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // 4. タイムアウトまたは失敗のチェック
        if (maxWait < 1)
        {
            UnityEngine.Debug.LogError("GPS初期化がタイムアウトしました。");
            yield break;
        }
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            UnityEngine.Debug.LogError("GPSデバイスとの接続に失敗しました。");
            yield break;
        }

        // 5. 成功: サービス更新を開始
        IsInitialized = true;
        UnityEngine.Debug.Log("GPSサービスが正常に開始されました。");

        // 定期的に位置を更新するループ
        while (IsInitialized)
        {
            UpdateGPSData();
            // リアルタイムに近い更新頻度で実行
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 現在のGPSデータを取得し、ローカル座標に変換して保存します。
    /// </summary>
    private void UpdateGPSData()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            // 生のGPSデータを取得
            LocationInfo location = Input.location.lastData;
            CurrentLatitude = location.latitude;
            CurrentLongitude = location.longitude;

            // ローカル座標に変換し、現在の位置を更新
            CurrentLocalPosition = ConvertGPSToLocal(CurrentLatitude, CurrentLongitude);
        }
    }

    /// <summary>
    /// 緯度経度を Unity シーン内のローカル座標 (X, 0, Z) に変換します。
    /// この関数は MapDataConverter でも使用されます。
    /// </summary>
    public Vector3 ConvertGPSToLocal(double latitude, double longitude)
    {
        // 緯度差（Z軸）: 北方向が正
        double deltaLat = latitude - ReferenceLatitude;
        float z = (float)(deltaLat * MetersPerDegreeLat);

        // 経度差（X軸）: 東方向が正
        double deltaLon = longitude - ReferenceLongitude;
        float x = (float)(deltaLon * MetersPerDegreeLon);

        // Y座標は高さ情報であり、ここでは常に 0 として扱います
        return new Vector3(x, 0, z);
    }

    void OnDestroy()
    {
        // シーン終了時にGPSサービスを停止
        Input.location.Stop();
    }
}