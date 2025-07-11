using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("会話ファイルの設定")]
    [SerializeField] private TextAsset defaultDialogue;
    [SerializeField] private TextMeshProUGUI textToEnableAfterDefault;

    [Header("特殊な条件での会話（任意）")]
    [SerializeField] private TextAsset specialDialogue;
    [SerializeField] private TextMeshProUGUI textToEnableAfterSpecial;

    private TVController tvController;
    private CurtainController curtainController;
    private CalendarController calendarController;
    private TelephoneController telephoneController;

    private void OnEnable()
    {
        DialogueManager.OnDialogueFinished += HandleDialogueFinished;
        // 同じオブジェクトにあるコンポーネントをまとめて取得
        tvController = GetComponent<TVController>();
        curtainController = GetComponent<CurtainController>();
        calendarController = GetComponent<CalendarController>();
        telephoneController = GetComponent<TelephoneController>();
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueFinished -= HandleDialogueFinished;
    }

    private void Start()
    {
        if (textToEnableAfterDefault != null)
        {
            textToEnableAfterDefault.gameObject.SetActive(false);
        }
        if (textToEnableAfterSpecial != null)
        {
            textToEnableAfterSpecial.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        //右クリックが押された瞬間じゃなければ、何もしない
        if (!Input.GetMouseButtonDown(1)) //1:右クリック
        {
            return;
        }

        // マウスカーソルの下にあるオブジェクトがこのオブジェクト自身かチェック
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider == null || hit.collider.gameObject != this.gameObject)
        {
            return;
        }

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
        // カレンダーがめくられているか？
        if (calendarController != null && calendarController.IsPageTurned)
        {
            return specialDialogue;
        }

        // カーテンが開いているか？
        if (curtainController != null && curtainController.IsOpen)
        {
            return specialDialogue;
        }

        // TVがオンか？
        if (tvController != null && tvController.IsTVOn)
        {
            return specialDialogue;
        }

        // もし電話コントローラーがあって録音が再生中なら
        if (telephoneController != null && telephoneController.IsPlayingRecording)
        {
            return specialDialogue;
        }

        // それ以外の場合はデフォルトの会話を返す
        return defaultDialogue;
    }

    private void HandleDialogueFinished(TextAsset finishedDialogue)
    {
        if (finishedDialogue == specialDialogue && textToEnableAfterSpecial != null)
        {
            textToEnableAfterSpecial.gameObject.SetActive(true);
        }
        else if (finishedDialogue == defaultDialogue && textToEnableAfterDefault != null)
        {
            textToEnableAfterDefault.gameObject.SetActive(true);
        }
    }
}