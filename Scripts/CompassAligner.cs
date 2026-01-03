using UnityEngine;
using System.Collections;

public class CompassAligner : MonoBehaviour
{
    [Tooltip("AR Session Origin または AR Camera の Transform")]
    public Transform targetTransform;
    [Tooltip("回転速度")]
    public float rotationSpeed = 1f;

    void Start()
    {
        // コンパスセンサーが有効になっているか確認
        if (!Input.location.isEnabledByUser)
        {
            UnityEngine.Debug.LogError("位置情報サービスが無効なため、コンパスを使用できません。");
            return;
        }

        // デバイスのコンパスを有効化
        Input.compass.enabled = true;
    }

    void Update()
    {
        if (Input.compass.enabled && targetTransform != null)
        {
            // 真北からの角度 (0° = 北, 90° = 東)
            float magneticHeading = Input.compass.magneticHeading;

            // OBJモデルはUnityワールドの +Z (前方) を真北に設定済み
            // ターゲットの現在のY軸回転を取得
            Quaternion currentRotation = targetTransform.rotation;

            // 目標とする回転 (magneticHeading を打ち消すように回転)
            // Z軸を北 (0°) に合わせるため、360度から heading を引くか、単純に heading をY軸回転として適用
            Quaternion targetRotation = Quaternion.Euler(0, magneticHeading, 0);

            // スムーズな回転
            targetTransform.rotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}