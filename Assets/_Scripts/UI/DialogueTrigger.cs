using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
/// <summary>
/// 右クリックで会話を開始するトリガー。Inspectorで会話ファイルや関連UIを設定し、終了時に表示切替や証拠のアンロックを行う。
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    /// <summary>
    /// どの会話終了時に証拠をアンロックするかを指定する列挙型。
    /// </summary>
    public enum ClueUnlockTrigger { OnDefaultDialogue, OnSpecialDialogue }

    [Header("会話ファイルの設定")]
    [SerializeField]
    /// <summary>
    /// デフォルトで再生する会話ファイル。InspectorでTextAssetを割り当てる。
    /// null時は会話を開始しない。
    /// </summary>
    private TextAsset defaultDialogue;

    [SerializeField]
    /// <summary>
    /// デフォルト会話終了後に有効化するUIテキスト（InspectorでD&D）。null許容。
    /// </summary>
    private TextMeshProUGUI textToEnableAfterDefault;

    [Header("特殊な条件での会話（任意）")]
    [SerializeField]
    /// <summary>
    /// 特殊条件時に再生する会話ファイル。条件が満たされない場合は使用されない。
    /// </summary>
    private TextAsset specialDialogue;

    [SerializeField]
    /// <summary>
    /// 特殊会話終了後に有効化するUIテキスト（InspectorでD&D）。null許容。
    /// </summary>
    private TextMeshProUGUI textToEnableAfterSpecial;

    [Header("証拠設定")]
    [Tooltip("どの会話が終わった時に証拠をアンロックするか")]
    /// <summary>
    /// 証拠アンロックのトリガー条件をInspectorで指定する。
    /// </summary>
    public ClueUnlockTrigger unlockTriggerCondition = ClueUnlockTrigger.OnDefaultDialogue;

    [Tooltip("この会話でアンロックされる証拠のリスト（任意）")]
    /// <summary>
    /// 会話終了時にアンロックする証拠のリスト。空またはnullなら何もしない。
    /// InspectorでClueアセットを割り当てる。
    /// </summary>
    public List<Clue> cluesToReveal;

    /// <summary>同一オブジェクト上に存在するTV制御用コンポーネントの参照。nullチェックあり。</summary>
    private TVController tvController;

    /// <summary>同一オブジェクト上に存在するカーテン制御用コンポーネントの参照。nullチェックあり。</summary>
    private CurtainController curtainController;

    /// <summary>同一オブジェクト上に存在するカレンダー制御用コンポーネントの参照。nullチェックあり。</summary>
    private CalendarController calendarController;

    /// <summary>同一オブジェクト上に存在する電話制御用コンポーネントの参照。nullチェックあり。</summary>
    private TelephoneController telephoneController;

    /// <summary>
    /// UnityのEnableイベント。イベント登録と同一オブジェクト上の関連コンポーネント取得を行う。
    /// OnEnableはオブジェクトが有効化されるたびに呼ばれるため、購読/取得処理はここで行う。
    /// </summary>
    private void OnEnable()
    {
        DialogueManager.OnDialogueFinished += HandleDialogueFinished;
        // 同じオブジェクトにあるコンポーネントをまとめて取得
        tvController = GetComponent<TVController>();
        curtainController = GetComponent<CurtainController>();
        calendarController = GetComponent<CalendarController>();
        telephoneController = GetComponent<TelephoneController>();
    }

    /// <summary>
    /// UnityのDisableイベント。イベント購読解除を行う。
    /// OnDisableはオブジェクトが無効化されるたびに呼ばれるため、必ず購読解除を行う。
    /// </summary>
    private void OnDisable()
    {
        DialogueManager.OnDialogueFinished -= HandleDialogueFinished;
    }

    /// <summary>
    /// UnityのStartイベント。インスペクタで割り当てたUIテキストを初期的に非表示にする。
    /// StartはOnEnableの後、最初のフレーム直前に呼ばれる初期化処理向け。
    /// </summary>
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

    /// <summary>
    /// Unityの毎フレームUpdate。右クリックによる会話開始を監視する。
    /// 入力が無効化されている場合は早期リターン。ScreenToWorldConverterでワールド座標変換に失敗した場合は何もしない。
    /// </summary>
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

    /// <summary>
    /// 現在の環境状態に応じて再生すべき会話ファイルを返す。
    /// カレンダーのページ、電話の録音再生などを優先して特殊会話を返す。条件が満たされない場合はデフォルト会話を返す。
    /// </summary>
    /// <returns>再生すべきTextAsset（null許容）。</returns>
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

    /// <summary>
    /// DialogueManagerから呼ばれる会話終了ハンドラ。
    /// このコンポーネントが開始した会話でない場合は処理を行わない。UIの有効化と、設定に応じた証拠のアンロックを行う。
    /// </summary>
    /// <param name="finishedDialogue">終了した会話のTextAsset。</param>
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