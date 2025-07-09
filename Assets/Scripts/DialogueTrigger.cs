using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("会話ファイルの設定")]
    [Tooltip("通常時、またはこのオブジェクトにメインで再生させたい会話")]
    [SerializeField] private TextAsset defaultDialogue;
    [Tooltip("通常会話が終わった後に有効化するTextMeshProオブジェクト（任意）")]
    [SerializeField] private TextMeshProUGUI textToEnableAfterDefault;

    [Header("特殊な条件での会話（任意）")]
    [SerializeField] private TextAsset specialDialogue;
    [Tooltip("特殊な会話が終わった後に有効化するTextMeshProオブジェクト（任意）")]
    [SerializeField] private TextMeshProUGUI textToEnableAfterSpecial;

    // --- 内部で使う変数 ---
    private TVController tvController;
    private CurtainController curtainController;

    private void OnEnable()
    {
        DialogueManager.OnDialogueFinished += HandleDialogueFinished;
        tvController = GetComponent<TVController>();
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueFinished -= HandleDialogueFinished;
    }

    private void Start()
    {
        // 開始時に、設定されていれば両方のTMPを非表示にしておく
        if (textToEnableAfterDefault != null)
        {
            textToEnableAfterDefault.gameObject.SetActive(false);
        }
        if (textToEnableAfterSpecial != null)
        {
            textToEnableAfterSpecial.gameObject.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        TextAsset dialogueToPlay = GetDialogueToPlay();

        if (dialogueToPlay != null && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            DialogueManager.GetInstance().EnterDialogueMode(dialogueToPlay);
        }
    }

    private TextAsset GetDialogueToPlay()
    {
        // もしカーテンコントローラーがあって、カーテンが開いているなら
        if (curtainController != null && curtainController.IsOpen)
        {
            // 特殊な会話を返す
            return specialDialogue;
        }

        // もしTVコントローラーがあって、TVがオンの状態なら
        if (tvController != null && tvController.IsTVOn)
        {
            // 特殊な会話を返す
            return specialDialogue;
        }

        // それ以外の場合は、デフォルトの会話を返す
        return defaultDialogue;
    }

    // ★★★ このメソッドのロジックを修正 ★★★
    // 「会話が終了した」という合図を受け取った時に呼ばれるメソッド
    private void HandleDialogueFinished(TextAsset finishedDialogue)
    {
        // 終了したのが「特殊な会話」だった場合
        if (finishedDialogue == specialDialogue && textToEnableAfterSpecial != null)
        {
            textToEnableAfterSpecial.gameObject.SetActive(true);
        }
        // 終了したのが「通常の会話」だった場合
        else if (finishedDialogue == defaultDialogue && textToEnableAfterDefault != null)
        {
            textToEnableAfterDefault.gameObject.SetActive(true);
        }
    }
}