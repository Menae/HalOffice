using UnityEngine;
using UnityEngine.EventSystems; // UIイベントを扱うために必須

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    private void OnMouseDown()
    {
        // ★★★ 変更：GameManagerに入力状態を確認 ★★★
        // GameManagerが存在しない、または入力が無効なら何もしない
        if (GameManager.Instance == null || !GameManager.Instance.isInputEnabled)
        {
            return;
        }

        // マウスポインターがUI要素の上にあれば何もしない
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (inkJSON == null)
        {
            Debug.LogWarning("DialogueTriggerにInk JSONファイルが設定されていません。", this.gameObject);
            return;
        }

        if (!DialogueManager.GetInstance().dialogueIsPlaying)
        {
            DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
        }
    }
}