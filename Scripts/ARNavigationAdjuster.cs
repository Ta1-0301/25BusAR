using UnityEngine;
using System.Collections.Generic;

public class ARNavigationAdjuster : MonoBehaviour
{
    [Header("References")]
    public PathfindingManager pathManager;
    public Transform cameraTransform;

    [Header("Scale Correction")]
    [Tooltip("モデルの1単位が実際の何メートルか(例: モデルが小さすぎるなら1.1、大きいなら0.9)")]
    public float scaleFactor = 1.0f;

    [Header("Drift Correction (Auto Snapping)")]
    [Tooltip("自動補正を有効にするか")]
    public bool enableAutoCorrection = true;
    [Tooltip("ルートから何メートル以上ズレたら補正するか")]
    public float driftThreshold = 2.0f;
    [Tooltip("補正の強さ (0.1 = ゆっくり吸い付く, 1.0 = 即座にワープ)")]
    [Range(0.01f, 1.0f)]
    public float correctionLerp = 0.05f;

    private Vector3 _lastTargetPosition;

    void Update()
    {
        if (pathManager == null || cameraTransform == null || !enableAutoCorrection) return;

        ApplyDriftCorrection();
    }

    /// <summary>
    /// ユーザーの現在位置とルート（直近の2点間）の距離を計算し、ズレを補正します。
    /// </summary>
    private void ApplyDriftCorrection()
    {
        // 1. 現在のナビゲーション目標（次の指示ポイント）を取得
        int targetIdx = pathManager.GetCurrentInstructionIndex();
        if (targetIdx <= 0 || targetIdx >= pathManager.FixedInstructions.Count) return;

        // ルートの線分（前のポイント A と 次のポイント B）
        Vector3 pointA = pathManager.FixedInstructions[targetIdx - 1].LocalPosition * scaleFactor;
        Vector3 pointB = pathManager.FixedInstructions[targetIdx].LocalPosition * scaleFactor;

        // 高さを無視した水平位置
        pointA.y = 0;
        pointB.y = 0;
        Vector3 playerPos = cameraTransform.position;
        playerPos.y = 0;

        // 2. 線分AB上で、現在地に最も近い点（スナップ先）を計算
        Vector3 nearestPoint = GetNearestPointOnLineSegment(pointA, pointB, playerPos);

        // 3. ズレ（距離）を計算
        float currentDrift = Vector3.Distance(playerPos, nearestPoint);

        if (currentDrift > driftThreshold)
        {
            // 4. AR Session Origin を動かして、カメラをルート上に引き戻す（オフセット計算）
            Vector3 driftOffset = nearestPoint - playerPos;

            // 徐々に補正をかける（急激な画面の揺れを防ぐ）
            transform.position += driftOffset * correctionLerp;

            Debug.Log($"[AR Adjuster] ルートからのズレ({currentDrift:F2}m)を検知。自動補正中...");
        }
    }

    // 線分上の最近接点を求める数学的処理
    private Vector3 GetNearestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;
        float magnitudeAB = ab.sqrMagnitude;
        float dotProduct = Vector3.Dot(ap, ab);
        float distance = dotProduct / magnitudeAB;

        if (distance < 0) return a;
        if (distance > 1) return b;
        return a + ab * distance;
    }
}