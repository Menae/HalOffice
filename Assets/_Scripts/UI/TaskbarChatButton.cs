using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TaskbarChatButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        // ボタンがクリックされた時にOnClickメソッドを呼び出すように自動で登録
        button.onClick.AddListener(OnClick);
    }

    // ボタンがクリックされた時の処理
    public void OnClick()
    {
        if (ChatController.Instance != null)
        {
            // ChatControllerのシングルトンインスタンスに命令を送る
            ChatController.Instance.ToggleChatWindow();
        }
        else
        {
            Debug.LogError("ChatController.Instanceが見つかりません！");
        }
    }
}