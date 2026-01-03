using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

// =========================================================================
// データ構造の定義
// =========================================================================

/// <summary>
/// 曲がる方向を定義する列挙型
/// </summary>
public enum TurnDirection
{
    STRAIGHT, // 直進
    LEFT,     // 左折
    RIGHT,    // 右折
    GOAL      // 到着
}

/// <summary>
/// ナビゲーションの指示情報を持つ構造体
/// </summary>
[System.Serializable]
public struct NavigationInstruction
{
    [Tooltip("曲がり角や重要な経由点のローカル座標")]
    public Vector3 LocalPosition;

    [Tooltip("この経由点で取るべき動作")]
    public TurnDirection Direction;

    [Tooltip("UIなどに表示する具体的な指示テキスト")]
    public string InstructionText;
}

public class PathfindingManager : MonoBehaviour
{
    // =========================================================================
    // Inspectorで設定する外部参照と設定
    // =========================================================================

    [Header("External References")]
    public GPSLocationProvider gpsProvider;
    public MapDataConverter mapConverter;

    [Header("OBJ Model Raycast Settings")]
    public LayerMask objModelLayerMask; // OBJモデルのレイヤー
    public float AR_ARROW_HEIGHT = 1.5f; // 地面から浮く高さ (m)

    [Header("Assets")]
    public GameObject arArrowPrefab;    // 通常の矢印
    public GameObject turnMarkerPrefab;  // 曲がり角/ゴール用マーカー
    public Transform arArrowsParent;   // 生成した矢印の親

    [Header("Camera Control")]
    [Tooltip("XR Origin (AR Rig) をアタッチ")]
    public Transform arSessionOrigin;

    [Header("Prototype Route (Fixed Instructions)")]
    public List<NavigationInstruction> FixedInstructions = new List<NavigationInstruction>
    {
        new NavigationInstruction {
            LocalPosition = new Vector3(332.1f, 0f, -221.4f),
            Direction = TurnDirection.STRAIGHT,
            InstructionText = "スタートしました。直進してください。"
        },
        new NavigationInstruction {
            LocalPosition = new Vector3(3f, 0f, 6.1f),
            Direction = TurnDirection.LEFT,
            InstructionText = "左折してください。"
        },
        new NavigationInstruction {
            LocalPosition = new Vector3(-85.5f, 0f, -60.5f),
            Direction = TurnDirection.GOAL,
            InstructionText = "目的地に到着しました。"
        }
    };

    [Header("Navigation Runtime Settings")]
    public float instructionDistanceThreshold = 5.0f;
    private List<Vector3> _smoothPath = new List<Vector3>();
    private int _currentInstructionIndex = 0;

    // =========================================================================
    // Unity ライフサイクル
    // =========================================================================

    void Start()
    {
        if (FixedInstructions.Count >= 2)
        {
            UnityEngine.Debug.Log("PathfindingManager: 初期化開始。ARマーカーを配置します。");

            _smoothPath = FixedInstructions.Select(i => i.LocalPosition).ToList();

            // 地面に沿ってマーカーを事前配置
            PlaceARMarkersAlongPath(FixedInstructions);
        }
        else
        {
            UnityEngine.Debug.LogError("ナビゲーションデータが不足しています。");
        }
    }

    void Update()
    {
        // ユーザーと指示ポイントの距離を監視
        if (_smoothPath.Count > 0 && arSessionOrigin != null && _currentInstructionIndex < FixedInstructions.Count)
        {
            Vector3 playerPos = arSessionOrigin.position;
            playerPos.y = 0;

            Vector3 instructionPos = FixedInstructions[_currentInstructionIndex].LocalPosition;
            instructionPos.y = 0;

            float distance = Vector3.Distance(playerPos, instructionPos);

            if (distance <= instructionDistanceThreshold)
            {
                if (distance < 1.2f) // 通過判定
                {
                    _currentInstructionIndex++;
                    UnityEngine.Debug.Log($"指示ポイント通過。次へ: {_currentInstructionIndex}");
                }
            }
        }
    }

    // =========================================================================
    // パブリック API (外部・UIからの呼び出し用)
    // =========================================================================

    /// <summary>
    /// 現在進行中の指示インデックスを返す (ARNavigationAdjuster用)
    /// </summary>
    public int GetCurrentInstructionIndex()
    {
        return _currentInstructionIndex;
    }

    public void StartNavigationFromUI()
    {
        StartNavigation(Vector2.zero);
    }

    public void StartNavigation(Vector2 destinationGPS)
    {
        if (_smoothPath.Count == 0) return;

        UnityEngine.Debug.Log("ナビゲーション開始。スタート地点へ移動します。");
        MoveCameraToStartPoint();
        _currentInstructionIndex = 0;
    }

    /// <summary>
    /// カメラをスタート地点へ移動させる (Raycastで地面の高さを考慮)
    /// </summary>
    public void MoveCameraToStartPoint()
    {
        if (arSessionOrigin == null || FixedInstructions.Count == 0) return;

        Vector3 startPos = FixedInstructions[0].LocalPosition;
        RaycastHit hit;

        // 空中から地面に向かってレイを飛ばす
        Vector3 rayStart = new Vector3(startPos.x, 100f, startPos.z);

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, objModelLayerMask))
        {
            Vector3 finalPos = hit.point;
            finalPos.y += AR_ARROW_HEIGHT;
            arSessionOrigin.position = finalPos;
            UnityEngine.Debug.Log("カメラを地面の高さに合わせて移動しました。");
        }
        else
        {
            arSessionOrigin.position = new Vector3(startPos.x, arSessionOrigin.position.y, startPos.z);
            UnityEngine.Debug.LogWarning("地面が見つからなかったため、現在の高さで移動しました。");
        }
    }

    // =========================================================================
    // ARマーカー生成ロジック
    // =========================================================================

    private void PlaceARMarkersAlongPath(List<NavigationInstruction> rawInstructions)
    {
        if (arArrowsParent == null || arArrowPrefab == null) return;

        // 既存のマーカーを削除
        foreach (Transform child in arArrowsParent)
        {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }

        float markerInterval = 1.5f;
        RaycastHit hit;

        for (int i = 0; i < rawInstructions.Count - 1; i++)
        {
            Vector3 startP = rawInstructions[i].LocalPosition;
            Vector3 endP = rawInstructions[i + 1].LocalPosition;
            float segmentDist = Vector3.Distance(startP, endP);

            // --- 1. 指示ポイント (曲がり角) ---
            Vector3 turnRayStart = new Vector3(startP.x, 100f, startP.z);
            if (Physics.Raycast(turnRayStart, Vector3.down, out hit, 200f, objModelLayerMask) && turnMarkerPrefab != null)
            {
                Vector3 finalPos = hit.point + Vector3.up * (AR_ARROW_HEIGHT * 1.5f);
                GameObject turnMarker = Instantiate(turnMarkerPrefab, finalPos, Quaternion.identity, arArrowsParent);

                Vector3 dir = (endP - startP).normalized;
                if (dir.sqrMagnitude > 0.001f) turnMarker.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            // --- 2. 通常の移動経路の矢印 ---
            float traveled = 0f;
            while (traveled < segmentDist)
            {
                Vector3 rawPos = Vector3.Lerp(startP, endP, traveled / segmentDist);
                Vector3 arrowRayStart = new Vector3(rawPos.x, 100f, rawPos.z);

                if (Physics.Raycast(arrowRayStart, Vector3.down, out hit, 200f, objModelLayerMask))
                {
                    Vector3 finalPos = hit.point + Vector3.up * AR_ARROW_HEIGHT;
                    GameObject arrow = Instantiate(arArrowPrefab, finalPos, Quaternion.identity, arArrowsParent);

                    Vector3 dir = (endP - startP).normalized;
                    if (dir.sqrMagnitude > 0.001f) arrow.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
                traveled += markerInterval;
            }
        }

        // --- 3. ゴール地点 ---
        Vector3 lastPos = rawInstructions.Last().LocalPosition;
        Vector3 goalRayStart = new Vector3(lastPos.x, 100f, lastPos.z);
        if (Physics.Raycast(goalRayStart, Vector3.down, out hit, 200f, objModelLayerMask) && turnMarkerPrefab != null)
        {
            Vector3 finalPos = hit.point + Vector3.up * (AR_ARROW_HEIGHT * 1.5f);
            Instantiate(turnMarkerPrefab, finalPos, Quaternion.identity, arArrowsParent);
        }
    }

    private void OnDrawGizmos()
    {
        if (FixedInstructions == null || FixedInstructions.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < FixedInstructions.Count - 1; i++)
        {
            Gizmos.DrawLine(FixedInstructions[i].LocalPosition + Vector3.up, FixedInstructions[i + 1].LocalPosition + Vector3.up);
        }
    }
}