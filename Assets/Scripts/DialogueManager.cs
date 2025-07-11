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

    public static event Action OnDialogueStart;
    public static event Action OnDialogueEnd;
    //どの会話ファイルが終了したかを通知するイベント
    public static event Action<TextAsset> OnDialogueFinished;

    private static DialogueManager instance;
    private TextAsset currentPlayingInk; //現在再生中のInkファイルを覚えておく

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

        choicesText = new TextMeshProUGUI[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            choicesText[i] = choices[i].GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentPlayingInk = inkJSON; //再生するファイルを覚えておく

        //GameManagerに入力停止を命令
        GameManager.Instance.SetInputEnabled(false);

        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        OnDialogueStart?.Invoke();
        StartCoroutine(StartDialogue());
    }


    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);

        //どの会話が終了したかをその会話ファイルと共に通知する
        OnDialogueFinished?.Invoke(currentPlayingInk);

        //GameManagerに入力再開を命令
        GameManager.Instance.SetInputEnabled(true);

        dialogueIsPlaying = false;
        dialogueText.text = "";

        if (typingAudioSource != null && typingAudioSource.isPlaying)
        {
            typingAudioSource.Stop();
        }

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
            //以前のテキスト表示コルーチンが残っていれば停止
            if (displayLineCoroutine != null)
            {
                StopCoroutine(displayLineCoroutine);
            }

            //canContinueToNextLineをfalseに設定しテキスト表示中は次の行に進めないようにする
            canContinueToNextLine = false;
            dialogueText.text = ""; //次の行を表示する前にテキストをクリア

            //新しいコルーチンを開始してテキストを一文字ずつ表示
            displayLineCoroutine = StartCoroutine(DisplayLine(currentStory.Continue().Trim()));

            HandleTags(currentStory.currentTags);
            DisplayChoices();

            //waitingForPlayerInput は DisplayLine コルーチンが終了した後に true に設定される
            //または、選択肢が表示された場合にここで true に設定する
            if (currentStory.currentChoices.Count > 0)
            {
                
            }
        }
        else
        {
            Debug.LogWarning("No more content to continue.");
            StartCoroutine(ExitDialogueMode());
        }
    }

    public void AdvanceDialogue()
    {
        //ダイアログが再生中でなければ何もしない
        if (!dialogueIsPlaying) return;

        //テキストがまだ表示中の場合 (タイプライター効果の途中)
        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            dialogueText.text = currentStory.currentText.Trim(); //全文を即座に表示
            displayLineCoroutine = null;
            canContinueToNextLine = true;
        }
        //テキストの表示が完了している場合
        else if (canContinueToNextLine)
        {
            ContinueStory(); //次の行に進む
        }
    }

    private IEnumerator DisplayLine(string line)
    {
        //テキストボックスを空にする
        dialogueText.text = "";
        //次の行に進めないように設定
        canContinueToNextLine = false;

        foreach (char letter in line.ToCharArray())
        {
            //文字が表示されるたびにサウンドエフェクトを再生
            if (typingAudioSource != null && typingSoundClip != null)
            {
                typingAudioSource.PlayOneShot(typingSoundClip, typingVolume);
            }

            dialogueText.text += letter; //一文字ずつ追加
            yield return new WaitForSeconds(typingSpeed); //指定されたタイピングスピードで待機
        }

        //全ての文字が表示されたら、次の行に進める状態にする
        canContinueToNextLine = true;

        //選択肢がない場合のみ、プレイヤーの入力待ち状態にする
        if (currentStory.currentChoices.Count == 0)
        {
            
        }
        else
        {
            //選択肢がある場合は、選択肢の表示後にwaitingForPlayerInputがtrueになるため、ここでは何もしない
        }

        displayLineCoroutine = null; //コルーチンが完了したら変数をリセット
    }

    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be parsed: " + tag);
                continue;
            }

            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            switch (tagKey)
            {
                case "speaker":
                    break;
                default:
                    Debug.LogWarning("Unhandled tag: " + tag);
                    break;
            }
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
        if (choiceIndex < 0 || choiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogError($"Invalid choice index: {choiceIndex}");
            return;
        }

        //選択時にタイピングコルーチンが実行中であれば停止
        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            displayLineCoroutine = null;
        }
        canContinueToNextLine = false; //新しいテキストが表示されるので、再度falseに設定

        EventSystem.current.SetSelectedGameObject(null);

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }
}