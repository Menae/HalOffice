using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タスクバーに配置されるチャットボタンの制御を行う。
/// クリック時にChatControllerのトグル処理を実行する。
/// </summary>
[RequireComponent(typeof(Button))]
public class TaskbarChatButton : MonoBehaviour
{
    private Button button;

    /// <summary>
    /// 初期化処理。
    /// Buttonコンポーネントを取得し、クリックイベントリスナーを登録する。
    /// </summary>
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// ボタンクリック時の処理。
    /// ChatControllerのシングルトンインスタンスを通じてチャットウィンドウの表示/非表示を切り替える。
    /// ChatControllerが存在しない場合はエラーログを出力する。
    /// </summary>
    public void OnClick()
    {
        if (ChatController.Instance != null)
        {
            ChatController.Instance.ToggleChatWindow();
        }
        else
        {
            Debug.LogError("ChatController.Instanceが見つかりません！");
        }
    }
}