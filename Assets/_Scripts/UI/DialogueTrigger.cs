using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    // 証拠をアンロックするタイミングを定義する
    public enum ClueUnlockTrigger { OnDefaultDialogue, OnSpecialDialogue }

    [Header("会話ファイルの設定")]
    [SerializeField] private TextAsset defaultDialogue;
    [SerializeField] private TextMeshProUGUI textToEnableAfterDefault;

    [Header("特殊な条件での会話（任意）")]
    [SerializeField] private TextAsset specialDialogue;
    [SerializeField] private TextMeshProUGUI textToEnableAfterSpecial;

    [Header("証拠設定")]
    [Tooltip("どの会話が終わった時に証拠をアンロックするか")]
    public ClueUnlockTrigger unlockTriggerCondition = ClueUnlockTrigger.OnDefaultDialogue;
    [Tooltip("この会話でアンロックされる証拠のリスト（任意）")]
    public List<Clue> cluesToReveal;

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
        if (!Input.GetMouseButtonDown(1)) return;
        if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

        // 新しい座標変換を使う
        if (ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                TextAsset dialogueToPlay = GetDialogueToPlay();
                if (dialogueToPlay != null && !DialogueManager.GetInstance().dialogueIsPlaying)
                {
                    DialogueManager.GetInstance().EnterDialogueMode(dialogueToPlay);
                }
            }
        }
    }

    private TextAsset GetDialogueToPlay()
    {
        // カレンダーがめくられているか？
        if (calendarController != null && calendarController.IsPageTurned)
        {
            return specialDialogue;
        }

        //// カーテンが開いているか？
        //if (curtainController != null && curtainController.IsOpen)
        //{
        //    return specialDialogue;
        //}

        //// TVがオンか？
        //if (tvController != null && tvController.IsTVOn)
        //{
        //    return specialDialogue;
        //}

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
        // このDialogueTriggerが開始した会話でなければ、何もしない
        if (finishedDialogue != defaultDialogue && finishedDialogue != specialDialogue)
        {
            return;
        }

        if (finishedDialogue == specialDialogue && textToEnableAfterSpecial != null)
        {
            textToEnableAfterSpecial.gameObject.SetActive(true);
        }
        else if (finishedDialogue == defaultDialogue && textToEnableAfterDefault != null)
        {
            textToEnableAfterDefault.gameObject.SetActive(true);
        }

        // 証拠をアンロックすべきか、設定された条件と終了した会話を元に判断する
        bool shouldUnlock = false;
        if (unlockTriggerCondition == ClueUnlockTrigger.OnDefaultDialogue && finishedDialogue == defaultDialogue)
        {
            shouldUnlock = true;
        }
        else if (unlockTriggerCondition == ClueUnlockTrigger.OnSpecialDialogue && finishedDialogue == specialDialogue)
        {
            shouldUnlock = true;
        }

        // アンロックすべき状況なら、リスト内の証拠をすべてアンロックする
        if (shouldUnlock && cluesToReveal != null && cluesToReveal.Count > 0)
        {
            foreach (Clue clue in cluesToReveal)
            {
                InvestigationManager.Instance.UnlockClue(clue);
            }
        }
    }
}