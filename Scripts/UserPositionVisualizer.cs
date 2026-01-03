using UnityEngine;

public class UserPositionVisualizer : MonoBehaviour
{
    public GPSLocationProvider gpsProvider;
    [Tooltip("ユーザーの位置を示す AR マーカーのプレハブ")]
    public GameObject userMarkerPrefab;

    private GameObject userMarker;

    void Start()
    {
        if (gpsProvider == null)
        {
            UnityEngine.Debug.LogError("GPSLocationProviderが割り当てられていません！");
            enabled = false;
            return;
        }

        // ユーザーマーカーをシーンに作成 (もしあれば)
        if (userMarkerPrefab != null)
        {
            userMarker = Instantiate(userMarkerPrefab, Vector3.zero, Quaternion.identity);
            userMarker.name = "User Position Marker";
        }
    }

    void Update()
    {
        if (gpsProvider.IsInitialized)
        {
            // GPSで取得したローカル座標にマーカーを移動
            Vector3 targetPos = gpsProvider.CurrentLocalPosition;

            // Y座標は、OBJモデルの高さに合わせるために Raycast が必要
            RaycastHit hit;
            if (Physics.Raycast(targetPos + Vector3.up * 50f, Vector3.down, out hit, 100f, LayerMask.GetMask("OBJModelLayer")))
            {
                targetPos.y = hit.point.y + 0.1f; // 地面よりわずかに浮かせる
            }
            // Raycastに失敗した場合は、そのままY=0として表示

            if (userMarker != null)
            {
                userMarker.transform.position = targetPos;
            }
        }
    }
}