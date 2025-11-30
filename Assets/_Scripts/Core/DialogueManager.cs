using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ink.Runtime;
using UnityEngine.EventSystems;

public class DialogueManager : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("UI要素")]
    [Tooltip("ダイアログの背景画像（任意）。設定されている場合、ダイアログ表示中のみ有効化されます。")]
    public GameObject dialogueBackground;

    [Header("連動オブジェクト (カーソル)")]
    [Tooltip("クリック待ちの時に表示するカーソルオブジェクト")]
    public GameObject objectToActivateDuringDialogue;

    [Header("カーソルアニメーション設定")]
    [Tooltip("カーソルが上下に動く幅")]
    [SerializeField] private float cursorMoveRange = 10f;
    [Tooltip("カーソルが動く周期の速さ")]
    [SerializeField] private float cursorMoveSpeed = 5f;
    [Tooltip("【レトロ風】動きを更新する間隔(秒)。0.1〜0.2くらいが昔のゲームっぽくなります。0にすると滑らかになります。")]
    [SerializeField] private float retroStepInterval = 0.15f;

    private RectTransform cursorRect;
    private Vector2 cursorOriginalPos;
    private float lastCursorUpdateTime; // アニメーション更新用タイマー

    private Coroutine displayLineCoroutine;
    private bool canContinueToNextLine = false;

    [Header("Audio")]
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingSoundClip;
    [Range(0f, 1f)][SerializeField] private float typingVolume = 0.5f;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    private TextMeshProUGUI[] choicesText;

    private Story currentStory;
    public bool dialogueIsPlaying { get; private set; }
    public static event Action<List<string>> OnTagsProcessed;
    public bool IsEffectPlaying { get; private set; }

    public static event Action OnDialogueStart;
    public static event Action OnDialogueEnd;
    public static event Action<TextAsset> OnDialogueFinished;

    private static DialogueManager instance;
    private TextAsset currentPlayingInk;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one DialogueManager found!");
        }
        instance = this;
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;

        if (dialogueBackground != null)
        {
            dialogueBackground.SetActive(false);
        }

        // カーソルオブジェクトの初期化
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

    private void Update()
    {
        // カーソルが有効な場合、レトロ風に上下アニメーションさせる
        if (dialogueIsPlaying && objectToActivateDuringDialogue != null && objectToActivateDuringDialogue.activeSelf)
        {
            // 設定した間隔（retroStepInterval）が経過した時だけ位置を更新する
            if (Time.time - lastCursorUpdateTime > retroStepInterval)
            {
                lastCursorUpdateTime = Time.time; // タイマー更新

                if (cursorRect != null)
                {
                    // Sin波を計算
                    float rawSin = Mathf.Sin(Time.time * cursorMoveSpeed);

                    // 値を丸めて整数（ピクセル単位）にする
                    float yOffset = Mathf.Round(rawSin * cursorMoveRange);

                    cursorRect.anchoredPosition = cursorOriginalPos + new Vector2(0, yOffset);
                }
            }
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentPlayingInk = inkJSON;
        GameManager.Instance.SetInputEnabled(false);

        // ここではまだカーソルを表示しない (テキスト表示完了を待つ)
        UpdateCursorState();

        SetDialogueBackgroundActive(true);

        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        OnDialogueStart?.Invoke();
        StartCoroutine(StartDialogue());
    }

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

        // カーソル非表示
        if (objectToActivateDuringDialogue != null)
        {
            objectToActivateDuringDialogue.SetActive(false);
        }

        SetDialogueBackgroundActive(false);

        Debug.Log("Dialogue ended. Enabling player controls.");
        OnDialogueEnd?.Invoke();
    }

    private IEnumerator StartDialogue()
    {
        yield return null;
        ContinueStory();
    }

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

    private IEnumerator DisplayLine(string line)
    {
        dialogueText.text = "";
        canContinueToNextLine = false;
        UpdateCursorState(); // 念のため非表示更新

        foreach (char letter in line.ToCharArray())
        {
            if (typingAudioSource != null && typingSoundClip != null)
            {
                typingAudioSource.PlayOneShot(typingSoundClip, typingVolume);
            }

            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        canContinueToNextLine = true;
        UpdateCursorState(); // テキスト表示完了、ここでカーソルが表示されるはず

        displayLineCoroutine = null;
    }

    private void HandleTags(List<string> currentTags)
    {
        if (currentTags.Count > 0)
        {
            OnTagsProcessed?.Invoke(currentTags);
        }
    }

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

        // 選択肢が出ている間はカーソルを隠したいので更新
        UpdateCursorState();

        StartCoroutine(SelectFirstChoice());
    }

    private IEnumerator SelectFirstChoice()
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        if (choices.Length > 0 && choices[0].activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
        }
    }

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
        UpdateCursorState(); // 選択した瞬間カーソル等は非表示

        EventSystem.current.SetSelectedGameObject(null);

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    public void SetIsPlayingEffect(bool isPlaying)
    {
        IsEffectPlaying = isPlaying;
        UpdateCursorState(); // エフェクト状態が変わったらカーソルの表示も更新
    }

    private void SetDialogueBackgroundActive(bool isActive)
    {
        if (dialogueBackground != null)
        {
            dialogueBackground.SetActive(isActive);
        }
    }

    /// <summary>
    /// 現在の状態に基づいてカーソルの表示/非表示を更新する
    /// 条件: 会話中 AND エフェクト再生中でない AND テキスト表示完了 AND 選択肢が出ていない
    /// </summary>
    private void UpdateCursorState()
    {
        if (objectToActivateDuringDialogue == null) return;

        // 会話中でないなら非表示
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