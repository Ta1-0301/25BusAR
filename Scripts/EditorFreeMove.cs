using UnityEngine;

/// <summary>
/// Unity Editor内でW/A/S/Dキーとマウスで自由にカメラを移動・回転させるためのスクリプト。
/// 右クリックを押している間だけカメラが回転し、それ以外はUI操作が可能です。
/// </summary>
public class EditorFreeMove : MonoBehaviour
{
    // Inspectorで設定
    [Header("Movement Settings")]
    public float movementSpeed = 5.0f;    // 移動速度
    public float rotationSpeed = 2.0f;    // マウス感度 (回転速度)
    
    // 回転のための内部状態
    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    void Start()
    {
        // 実行開始時、UI操作ができるようにカーソルを解放しておく
        if (Application.isEditor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // 現在の回転状態を初期化
            rotationX = transform.localEulerAngles.y;
            rotationY = transform.localEulerAngles.x;
        }
    }

    void Update()
    {
        // ARデバイス (実機) で実行している場合は、このスクリプトを無効化する
        if (!Application.isEditor) return;

        // 右クリックが押されているかを確認
        bool isRightMouseButtonHeld = Input.GetMouseButton(1); // 1 = 右クリック

        // --- 1. 回転処理 (マウスルック) ---
        if (isRightMouseButtonHeld)
        {
            // 右クリックが押されている間だけカーソルをロックし、非表示にする
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // マウスの動きを取得して回転
            rotationX += Input.GetAxis("Mouse X") * rotationSpeed;
            rotationY -= Input.GetAxis("Mouse Y") * rotationSpeed; 
            
            // 垂直方向の回転を制限
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            // 実際に回転
            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0.0f);
        }
        else
        {
            // 右クリックが離されているときは、カーソルを解放してUI操作を可能にする
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // ESCキーでの強制解除（保険）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // --- 2. 移動処理 (W/A/S/D と Q/E) ---
        // 移動処理は、カーソル状態に関わらず実行できるようにしておく
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");    // W/S
        
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical);
        moveDirection = transform.rotation * moveDirection;

        // 上下移動 (Q/Eキー)
        if (Input.GetKey(KeyCode.Q))
        {
            moveDirection += Vector3.down;
        }
        if (Input.GetKey(KeyCode.E))
        {
            moveDirection += Vector3.up;
        }
        
        // シフトキーを押している間は加速
        float speed = Input.GetKey(KeyCode.LeftShift) ? movementSpeed * 2.5f : movementSpeed;

        // 実際に移動
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnDisable()
    {
        // スクリプトが無効になったらカーソルを元に戻す
        if (Application.isEditor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}