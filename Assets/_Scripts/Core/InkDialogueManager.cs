using Ink.Runtime;
using System.Collections; // Coroutineを使うために必要
using TMPro;
using UnityEngine;

public class InkDialogueManager : MonoBehaviour
{
    public static InkDialogueManager Instance { get; private set; }

    [Header("UI参照")]
    [Tooltip("テキストを表示するTextMeshProコンポーネント")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Tooltip("ダイアログ再生中にのみアクティブにするオブジェクト（任意）")]
    [SerializeField] private GameObject objectToActivateDuringDialogue;

    [Header("演出設定")]
    [Tooltip("テキストが1文字ずつ表示される速さ")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private AudioSource typingAudioSource;
    [SerializeField] private AudioClip typingSoundClip;
    [Range(0f, 1f)][SerializeField] private float typingVolume = 0.5f;

    // --- 内部変数 ---
    private Story currentStory;
    private Coroutine displayLineCoroutine;
    public bool isStoryPlaying { get; private set; } // 外部が再生中か確認するためのフラグ

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); }
        else { Instance = this; }
    }

    private void Start()
    {
        // パネルは常に表示されているので、ここでは何もしない
        // dialogueTextの内容は空にしておく
        dialogueText.text = "";
        if (objectToActivateDuringDialogue != null)
        {
            objectToActivateDuringDialogue.SetActive(false);
        }
    }

    /// <summary>
    /// 新しいストーリーの表示を開始する
    /// </summary>
    public void ShowStory(TextAsset inkJSON)
    {
        if (inkJSON == null) return;

        isStoryPlaying = true;
        currentStory = new Story(inkJSON.text);

        if (objectToActivateDuringDialogue != null)
        {
            objectToActivateDuringDialogue.SetActive(true);
        }

        ContinueStory();
    }

    /// <summary>
    /// ストーリーを一行進める（クリックなどで呼び出す）
    /// </summary>
    public void ContinueStory()
    {
        // タイプライターの途中で呼び出されたら、全文を即時表示する
        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            dialogueText.text = currentStory.currentText.Trim();
            displayLineCoroutine = null; // コルーチン参照をリセット
            return;
        }

        if (currentStory != null && currentStory.canContinue)
        {
            displayLineCoroutine = StartCoroutine(DisplayLine(currentStory.Continue().Trim()));
        }
        else
        {
            CloseDialogue();
        }
    }

    /// <summary>
    /// ダイアログを終了する
    /// </summary>
    public void CloseDialogue()
    {
        if (displayLineCoroutine != null)
        {
            StopCoroutine(displayLineCoroutine);
            displayLineCoroutine = null;
        }

        isStoryPlaying = false;
        dialogueText.text = ""; // テキストを空にする
        currentStory = null;

        if (objectToActivateDuringDialogue != null)
        {
            objectToActivateDuringDialogue.SetActive(false);
        }
    }

    /// <summary>
    /// テキストを1文字ずつ表示するコルーチン
    /// </summary>
    private IEnumerator DisplayLine(string line)
    {
        dialogueText.text = ""; // テキストを一旦クリア

        foreach (char letter in line.ToCharArray())
        {
            // 効果音を再生
            if (typingAudioSource != null && typingSoundClip != null)
            {
                typingAudioSource.PlayOneShot(typingSoundClip, typingVolume);
            }
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        displayLineCoroutine = null; // コルーチンが完了したら参照をリセット
    }
}