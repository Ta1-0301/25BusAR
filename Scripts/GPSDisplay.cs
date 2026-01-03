using UnityEngine;

public class GPSDisplay : MonoBehaviour
{
    // GPSLocationProvider への参照
    public GPSLocationProvider gpsProvider;

    // 表示用のスタイル
    private GUIStyle labelStyle;

    void Start()
    {
        // スタイルを初期化
        labelStyle = new GUIStyle
        {
            fontSize = 30, // 大きめのフォントサイズ
            normal = new GUIStyleState { textColor = Color.white } // 白文字
        };

        if (gpsProvider == null)
        {
            UnityEngine.Debug.LogError("GPSLocationProviderが割り当てられていません。Inspectorで設定してください。");
            enabled = false;
        }
    }

    // 画面上にGUI要素を描画するUnityの特別な関数
    void OnGUI()
    {
        if (gpsProvider == null)
            return;

        string displayMessage;

        // GPSの初期化状態に基づきメッセージを構築
        if (gpsProvider.IsInitialized)
        {
            // 初期化が完了している場合、座標を表示
            displayMessage = "GPS Status: ✅ RUNNING\n";
            displayMessage += $"Lat: {gpsProvider.CurrentLatitude:F6}\n";
            displayMessage += $"Lon: {gpsProvider.CurrentLongitude:F6}\n";

            // ローカル座標も表示 (デバッグ用)
            Vector3 localPos = gpsProvider.CurrentLocalPosition;
            displayMessage += $"Local Pos (X, Z): {localPos.x:F2}, {localPos.z:F2}";
        }
        else
        {
            // 初期化中の場合、待機メッセージを表示
            LocationServiceStatus status = Input.location.status;

            // ステータスに応じたメッセージ
            if (status == LocationServiceStatus.Initializing)
            {
                displayMessage = "GPS Status: ⚠️ Initializing...";
            }
            else if (status == LocationServiceStatus.Stopped)
            {
                displayMessage = "GPS Status: ❌ Stopped (Need to start service)";
            }
            else if (status == LocationServiceStatus.Failed)
            {
                displayMessage = "GPS Status: 🚨 Failed to connect";
            }
            else
            {
                displayMessage = "GPS Status: ⏳ Waiting for status...";
            }
        }

        // 画面左上にテキストを描画
        GUI.Label(new Rect(10, 10, 500, 200), displayMessage, labelStyle);
    }
}