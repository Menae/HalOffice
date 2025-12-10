using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 会話フローの管理。InkのStoryを用いてテキスト表示、選択肢表示、タグ処理を行う。
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("Params")]
    [SerializeField]
    private float typingSpeed = 0.04f; // 1文字あたりの表示遅延（秒）。Inspectorで調整可能。

    [SerializeField]
    private TextMeshProUGUI dialogueText; // 表示先のTextMeshProUGUI。InspectorでD&Dする。

    [Header("UI要素")]
    [Tooltip("ダイアログの背景画像（任意）。設定されている場合、ダイアログ表示中のみ有効化されます。")]
    public GameObject dialogueBackground; // ダイアログ背景。null許容。

    [Header("連動オブジェクト (カーソル)")]
    [Tooltip("クリック待ちの時に表示するカーソルオブジェクト")]
    public GameObject objectToActivateDuringDialogue; // 次へ進める状態を示すカーソル。選択肢表示時は非表示。

    [Header("カーソルアニメーション設定")]
    [Tooltip("カーソルが上下に動く幅")]
    [SerializeField]
    private float cursorMoveRange = 10f; // カーソルの垂直移動幅（ピクセル相当）。

    [Tooltip("カーソルが動く周期の速さ")]
    [SerializeField]
    private float cursorMoveSpeed = 5f; // カーソルの揺れ速さ。

    [Tooltip("【レトロ風】動きを更新する間隔(秒)。0.1〜0.2くらいが昔のゲームっぽくなります。0にすると滑らかになります。")]
    [SerializeField]
    private float retroStepInterval = 0.15f; // 位置更新の間隔。0で毎フレーム更新。

    private RectTransform cursorRect;
    private Vector2 cursorOriginalPos;
    private float lastCursorUpdateTime; // アニメーション更新用タイマー

    private Coroutine displayLineCoroutine;
    public bool canContinueToNextLine = false;

    [Header("Audio")]
    [SerializeField]
    private AudioSource typingAudioSource; // 文字打ち音用AudioSource。無い場合は音が鳴らない。

    [SerializeField]
    private AudioClip typingSoundClip; // 1文字分の効果音。null可。

    [Range(0f, 1f)]
    [SerializeField]
    private float typingVolume = 0.5f; // 効果音の再生音量。

    [Header("Choices UI")]
    [SerializeField]
    private GameObject[] choices; // 選択肢ボタン群。Inspectorで配列サイズを合わせる。

    private TextMeshProUGUI[] choicesText;

    private Story currentStory;
    public bool dialogueIsPlaying { get; private set; }

    /// <summary>
    /// Inkタグ処理完了時に送出。引数は現在のタグ文字列リスト。
    /// </summary>
    public static event Action<List<string>> OnTagsProcessed;

    /// <summary>
    /// 会話進行中にゲーム上でエフェクト再生があるかどうかを示す。
    /// 外部からSetIsPlayingEffectで制御する。
    /// </summary>
    public bool IsEffectPlaying { get; private set; }

    /// <summary>
    /// 会話開始時に送出。UIやゲーム状態の初期化に利用。
    /// </summary>
    public static event Action OnDialogueStart;

    /// <summary>
    /// 会話終了時に送出。プレイヤー操作の復帰などに利用。
    /// </summary>
    public static event Action OnDialogueEnd;

    /// <summary>
    /// 1行の表示が完了した直後に送出。次へ進めるタイミングの通知に利用。
    /// </summary>
    public static event Action OnLineFinishDisplaying;

    /// <summary>
    /// Inkテキストアセットを再生し終えたときに送出。引数は再生していたTextAsset。
    /// </summary>
    public static event Action<TextAsset> OnDialogueFinished;

    private static DialogueManager instance;
    private TextAsset currentPlayingInk;

    /// <summary>
    /// Awakeイベント。インスタンスの単一化（シングルトン）を行う。
    /// AwakeはStartより先に呼ばれるため、他の初期化がStartで行われる場合は順序に注意。
    /// </summary>
    private void Awake()
    {
        // 加算ロードに対応：既存のインスタンスがあれば上書きし、警告は出さない。
        if (instance != null)
        {
            // 意図的な重複を許容するため警告を出さない。
        }
        instance = this;
    }

    /// <summary>
    /// シーン破棄時の処理。現在のインスタンスが自分であればクリアする。
    /// </summary>
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// DialogueManagerの現在のインスタンスを取得。存在しなければシーンから探して再割り当てする。
    /// </summary>
    /// <returns>既存のDialogueManagerインスタンス（見つからない場合はnull）。</returns>
    public static DialogueManager GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<DialogueManager>();
        }
        return instance;
    }

    /// <summary>
    /// Startイベント。Inspectorで設定されたUI要素や選択肢の初期参照を確保する。
    /// StartはAwakeの後、最初のフレーム更新前に呼ばれる。
    /// </summary>
    private void Start()
    {
        dialogueIsPlaying = false;

        if (dialogueBackground != null)
        {
            dialogueBackground.SetActive(false);
        }

        // カーソルオブジェクトの初期化。RectTransformが存在する場合は初期位置を記録。
        if (objectToActivateDuringDialogue != null)
        {
            cursorRect = objectToActivateDuringDialogue.GetComponent<RectTransform>();
            if (cursorRect != null)
            {
                cursorOriginalPos = cursorRect.anchoredPosition;
            }
            objectToActivateDuringDialogue.SetActive(false); // 最初は非表示
        }

        choicesText = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            choicesText[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Updateイベント。カーソルのレトロ風アニメーションを定期更新する。
    /// Updateは毎フレーム呼ばれるため、重い処理は避ける。
    /// </summary>
    private void Update()
    {
        // カーソルが有効かつ表示中であれば、retroStepIntervalに従って位置を更新する。
        if (dialogueIsPlaying && objectToActivateDuringDialogue != null && objectToActivateDuringDialogue.activeSelf)
        {
            if (Time.time - lastCursorUpdateTime > retroStepInterval)
            {
                lastCursorUpdateTime = Time.time;

                if (cursorRect != null)
                {
                    float rawSin = Mathf.Sin(Time.time * cursorMoveSpeed);
                    float yOffset = Mathf.Round(rawSin * cursorMoveRange);
                    cursorRect.anchoredPosition = cursorOriginalPos + new Vector2(0, yOffset);
                }
            }
        }
    }

    /// <summary>
    /// 指定したInk JSON（TextAsset）から会話を開始。プレイヤー入力を無効化して会話モードに遷移する。
    /// </summary>
    /// <param name="inkJSON">再生するInkのTextAsset（.ink.json）。</param>
    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentPlayingInk = inkJSON;
        GameManager.Instance.SetInputEnabled(false);

        // テキスト表示完了までカーソルは表示しないため状態更新
        UpdateCursorState();

        SetDialogueBackgroundActive(true);

        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        OnDialogueStart?.Invoke();
        StartCoroutine(StartDialogue());
    }

    /// <summary>
    /// 会話終了処理。少し待機してから状態を復帰する。
    /// </summary>
    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);

        OnDialogueFinished?.Invoke(currentPlayingInk);
        GameManager.Instance.SetInputEnabled(true);

        dialogueIsPlaying = false;
        IsEffectPlaying = false;

        dialogueText.text = "";

        if (typingAudioSource != null && typingAudioSource.isPlaying)
        {
            typingAudioSource.Stop();
        }

        if (objectToActivateDuringDialogue != null)
        {
            objectToActivateDuringDialogue.SetActive(false);
        }

        SetDialogueBackgroundActive(false);

        Debug.Log("Dialogue ended. Enabling player controls.");
        OnDialogueEnd?.Invoke();
    }

    /// <summary>
    /// StartCoroutineで呼び出される初期化コルーチン。次行へ進める処理を呼び出す。
    /// </summary>
    private IEnumerator StartDialogue()
    {
        yield return null;
        ContinueStory();
    }

    /// <summary>
    /// Inkのストーリーを進行させ、次の行の表示または会話終了を処理する。
    /// </summary>
    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            if (displayLineCoroutine != null)
            {
                StopCoroutine(displayLineCoroutine);
            }

            canContinueToNextLine = false;
            UpdateCursorState(); // テキスト表示中はカーソルを隠す

            dialogueText.text = "";

            string nextLine = currentStory.Continue().Trim();

            if (string.IsNullOrEmpty(nextLine))
            {
                SetDialogueBackgroundActive(false);
            }
            else
            {
                SetDialogueBackgroundActive(true);
            }

            displayLineCoroutine = StartCoroutine(DisplayLine(nextLine));

            HandleTags(currentStory.currentTags);
            DisplayChoices();
        }
        else
        {
            Debug.LogWarning("No more content to continue.");
            StartCoroutine(ExitDialogueMode());
        }
    }

    /// <summary>
    /// ユーザー入力（進行操作）を受け取ったときに呼び出す。表示中の文字を一括で表示するか次行へ進める。
    /// </summary>
    public void AdvanceDialogue()
    {
        if (!dialogueIsPlaying) return;
        if (IsEffectPlaying) return;

        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            dialogueText.text = currentStory.currentText.Trim();
            displayLineCoroutine = null;
            canContinueToNextLine = true;
            UpdateCursorState(); // 表示完了したので条件次第でカーソル表示
        }
        else if (canContinueToNextLine)
        {
            ContinueStory();
        }
    }

    /// <summary>
    /// 1行分のテキストをタイプライター風に表示するコルーチン。行の改行タグは置換して表示。
    /// </summary>
    /// <param name="line">表示する行テキスト（タグは事前に処理済みでなくてよい）。</param>
    private IEnumerator DisplayLine(string line)
    {
        dialogueText.text = "";
        canContinueToNextLine = false;
        UpdateCursorState();

        line = line.Replace("<br>", "\n");

        foreach (char letter in line.ToCharArray())
        {
            if (typingAudioSource != null && typingSoundClip != null)
            {
                typingAudioSource.PlayOneShot(typingSoundClip, typingVolume);
            }

            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // 文字表示が終わったのでイベント通知
        OnLineFinishDisplaying?.Invoke();

        canContinueToNextLine = true;
        UpdateCursorState();

        displayLineCoroutine = null;
    }

    /// <summary>
    /// Inkのタグリストをハンドルし、登録されたリスナへ通知する。
    /// </summary>
    /// <param name="currentTags">現在の行に付随するタグのリスト。</param>
    private void HandleTags(List<string> currentTags)
    {
        if (currentTags.Count > 0)
        {
            OnTagsProcessed?.Invoke(currentTags);
        }
    }

    /// <summary>
    /// 現在の選択肢をUIに反映し、最初の選択肢を選択状態にするコルーチンを起動する。
    /// </summary>
    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;
        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError($"Too many choices given: {currentChoices.Count}");
        }

        int index = 0;
        foreach (Choice choice in currentChoices)
        {
            choices[index].SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        for (int i = index; i < choices.Length; i++)
        {
            choices[i].SetActive(false);
        }

        // 選択肢が出ている間はカーソル非表示にするため状態更新
        UpdateCursorState();

        StartCoroutine(SelectFirstChoice());
    }

    /// <summary>
    /// フレーム終了後に最初の選択肢を選択状態にするコルーチン。EventSystemの設定を行う。
    /// </summary>
    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        if (choices.Length > 0 && choices[0].activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
        }
    }

    /// <summary>
    /// 指定した選択肢を選択してストーリーを進める。インデックスは0開始。
    /// </summary>
    /// <param name="choiceIndex">選択する選択肢のインデックス。</param>
    public void MakeChoice(int choiceIndex)
    {
        if (IsEffectPlaying) return;

        if (choiceIndex < 0 || choiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogError($"Invalid choice index: {choiceIndex}");
            return;
        }

        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            displayLineCoroutine = null;
        }
        canContinueToNextLine = false;
        UpdateCursorState(); // 選択した瞬間はカーソル等を非表示

        EventSystem.current.SetSelectedGameObject(null);

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    /// <summary>
    /// 外部からエフェクト再生中フラグを設定し、カーソル表示状態を更新する。
    /// </summary>
    /// <param name="isPlaying">エフェクト再生中ならtrue。</param>
    public void SetIsPlayingEffect(bool isPlaying)
    {
        IsEffectPlaying = isPlaying;
        UpdateCursorState(); // エフェクト状態の変化に応じてカーソル表示を更新
    }

    /// <summary>
    /// ダイアログ背景のアクティブ状態を切り替える。dialogueBackgroundがnullの場合は無視する。
    /// </summary>
    /// <param name="isActive">表示するならtrue。</param>
    private void SetDialogueBackgroundActive(bool isActive)
    {
        if (dialogueBackground != null)
        {
            dialogueBackground.SetActive(isActive);
        }
    }

    /// <summary>
    /// 現在の状態に基づいてカーソルの表示/非表示を更新する。
    /// 条件: 会話中 AND エフェクト再生中でない AND テキスト表示完了 AND 選択肢が出ていない。
    /// </summary>
    private void UpdateCursorState()
    {
        if (objectToActivateDuringDialogue == null) return;

        if (!dialogueIsPlaying)
        {
            objectToActivateDuringDialogue.SetActive(false);
            return;
        }

        // 選択肢が表示されているかチェック
        bool choicesAreActive = (currentStory != null && currentStory.currentChoices.Count > 0);

        // 表示条件:
        // 1. エフェクト再生中でない
        // 2. 次の行へ進める状態（テキスト表示完了）
        // 3. 選択肢が表示されていない
        bool shouldShow = !IsEffectPlaying && canContinueToNextLine && !choicesAreActive;

        objectToActivateDuringDialogue.SetActive(shouldShow);
    }
}