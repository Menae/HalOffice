using Ink.Runtime;
using System.Collections; // Coroutineを使うために必要
using TMPro;
using UnityEngine;

/// <summary>
/// Inkのストーリーを表示するダイアログ管理クラス。Singletonで管理。
/// ダイアログのタイプライター表示、オブジェクトのアクティベート、効果音再生を管理する。
/// Awakeでインスタンスを確立し、StartでUI初期化を行う。
/// </summary>
public class InkDialogueManager : MonoBehaviour
{
    /// <summary>
    /// Awakeで初期化されるシングルトンインスタンス。複数存在する場合は追加のインスタンスを破棄する。
    /// </summary>
    public static InkDialogueManager Instance { get; private set; }

    [Header("UI参照")]
    [Tooltip("テキストを表示するTextMeshProコンポーネント")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    // InspectorでTextMeshProUGUIをアサイン。nullなら表示操作は実行されない。

    [Tooltip("ダイアログ再生中にのみアクティブにするオブジェクト（任意）")]
    [SerializeField] private GameObject objectToActivateDuringDialogue;
    // Inspectorで対象オブジェクトをD&D。未設定時は無視。

    [Header("演出設定")]
    [Tooltip("テキストが1文字ずつ表示される速さ")]
    [SerializeField] private float typingSpeed = 0.04f;
    // Inspectorで再生速度を調整。値が小さいほど高速表示。

    [SerializeField] private AudioSource typingAudioSource;
    // InspectorでAudioSourceをアサイン。効果音再生に使用。未設定時は効果音再生を行わない。

    [SerializeField] private AudioClip typingSoundClip;
    [Range(0f, 1f)][SerializeField] private float typingVolume = 0.5f;
    // 効果音のクリップと音量。音量は0.0〜1.0で指定。

    // --- 内部変数 ---
    private Story currentStory;
    private Coroutine displayLineCoroutine;
    /// <summary>
    /// ストーリー再生中フラグ。外部から再生状態を確認する目的で参照可能。
    /// </summary>
    public bool isStoryPlaying { get; private set; } // 外部が再生中か確認するためのフラグ

    private void Awake()
    {
        // MonoBehaviourの初期化フェーズで呼ばれる。シングルトンの確立と重複破棄を行う。
        if (Instance != null) { Destroy(gameObject); }
        else { Instance = this; }
    }

    private void Start()
    {
        // Scene開始時にUIを初期化。dialogueTextの内容を空にする。
        dialogueText.text = "";
        if (objectToActivateDuringDialogue != null)
        {
            objectToActivateDuringDialogue.SetActive(false);
        }
    }

    /// <summary>
    /// 指定したInk JSON(TextAsset)から新しいストーリー再生を開始する。
    /// ストーリーをパースして内部状態を初期化し、必要ならオブジェクトをアクティブ化して最初の行を表示する。
    /// 引数がnullの場合は何もしない。
    /// </summary>
    /// <param name="inkJSON">Inkエクスポート済みのJSONを含むTextAsset。nullなら処理を中断。</param>
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
    /// ストーリーを1行進める。ユーザー入力（クリック等）から呼び出すことを想定。
    /// 既にタイプライター表示中に呼ばれた場合は、その行を即時表示してコルーチンを停止する。
    /// 表示可能な行が無ければダイアログを閉じる。
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
    /// 現在のダイアログ再生を終了する。
    /// コルーチン停止、UIクリア、内部ストーリー参照の破棄、関連オブジェクトの非アクティブ化を行う。
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
    /// 指定した文字列を1文字ずつ表示するコルーチン。文字ごとに効果音を再生する（AudioSourceとクリップが設定されている場合）。
    /// WaitForSecondsで表示間隔を制御するため、タイミングはTime.timeScaleの影響を受ける。
    /// </summary>
    /// <param name="line">画面に表示する行テキスト。事前にTrim済みで渡される。</param>
    /// <returns>IEnumerator（コルーチン）</returns>
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