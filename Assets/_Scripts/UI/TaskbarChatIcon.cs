using UnityEngine;

public class TaskbarChatIcon : MonoBehaviour
{
    public void OnIconClicked()
    {
        // 処理を全てChatControllerに委任する
        if (ChatController.Instance != null)
        {
            ChatController.Instance.ToggleChatWindow();
        }
    }
}