using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // TextMeshProを扱うために必須

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("会話ファイルの設定")]
    [Tooltip("通常時、またはこのオブジェクトにメインで再生させたい会話")]
    [SerializeField] private TextAsset defaultDialogue;

    [Header("特殊な条件での会話（任意）")]
    [SerializeField] private TextAsset specialDialogue;
    // ★★★ この2行を追加 ★★★
    [Tooltip("特殊な会話が終わった後に有効化するTextMeshProオブジェクト")]
    [SerializeField] private TextMeshProUGUI textToEnableAfterSpecial;

    // --- 内部で使う変数 ---
    private TVController tvController;

    // ★★★ Awakeメソッドを、OnEnableとOnDisableに変更 ★★★
    private void OnEnable()
    {
        // DialogueManagerからの「会話終了」の合図を受け取る準備
        DialogueManager.OnDialogueFinished += HandleDialogueFinished;

        // 同じオブジェクトにTVControllerがあれば、それを取得しておく
        tvController = GetComponent<TVController>();
    }

    private void OnDisable()
    {
        // このオブジェクトが非表示になる時に、合図の受け取りを解除する（お行儀の良いお作法よ）
        DialogueManager.OnDialogueFinished -= HandleDialogueFinished;
    }

    private void Start()
    {
        // ★★★ 追加：開始時にTMPを非表示にしておく ★★★
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
        if (tvController != null && tvController.IsTVOn)
        {
            return specialDialogue;
        }
        return defaultDialogue;
    }

    // ★★★ このメソッドを丸ごと追加 ★★★
    // 「会話が終了した」という合図を受け取った時に呼ばれるメソッド
    private void HandleDialogueFinished(TextAsset finishedDialogue)
    {
        // 終了した会話が、このトリガーが持つ「特殊な会話」と同じもので、
        // かつ、有効化するTMPが設定されていれば…
        if (finishedDialogue == specialDialogue && textToEnableAfterSpecial != null)
        {
            // そのTMPを有効化する
            textToEnableAfterSpecial.gameObject.SetActive(true);
        }
    }
}