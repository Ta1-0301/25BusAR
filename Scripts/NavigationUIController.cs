using UnityEngine;
using UnityEngine.UI; // ButtonやDropdownを使う場合に必要

public class NavigationUIController : MonoBehaviour
{
    // --- Inspectorで設定する外部参照 ---
    [Header("Manager References")]
    public PathfindingManager pathfindingManager;

    [Header("UI References")]
    [Tooltip("ナビゲーション開始をトリガーするUIボタン")]
    // ⭐ このボタン自体を非表示の対象にします
    public Button startNavigationButton; 
    
    // ... (既存の Start メソッド) ...

    void Start()
    {
        // 参照チェック
        if (pathfindingManager == null)
        {
            UnityEngine.Debug.LogError("Pathfinding Managerが割り当てられていません。Inspectorを確認してください。");
        }

        // ボタンのOnClickイベントに関数を接続
        if (startNavigationButton != null)
        {
            startNavigationButton.onClick.AddListener(OnStartNavigationButtonClicked);
        }
    }


    /// <summary>
    /// ナビゲーション開始ボタンがクリックされたときに呼び出される関数。
    /// UIのOnClickイベントに接続します。
    /// </summary>
    public void OnStartNavigationButtonClicked()
    {
        if (pathfindingManager == null)
        {
            UnityEngine.Debug.LogError("Pathfinding ManagerがNULLのため、ナビゲーションを開始できません。");
            return;
        }

        // 1. PathfindingManagerのナビゲーション処理をトリガー
        Vector2 dummyDestinationGPS = Vector2.zero; 
        pathfindingManager.StartNavigation(dummyDestinationGPS);
        
        UnityEngine.Debug.Log("UIからナビゲーション開始を指示しました。");

        // 2. ⭐ ボタンを非表示にする処理
        if (startNavigationButton != null)
        {
            // ボタンの親オブジェクト（通常はボタン自体）を非アクティブ化する
            startNavigationButton.gameObject.SetActive(false);
            UnityEngine.Debug.Log("ナビ開始ボタンを非表示にしました。");
        }
    }
}