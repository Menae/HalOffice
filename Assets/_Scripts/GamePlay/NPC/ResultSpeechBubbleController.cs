using Ink.Runtime;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// 結果表示用の吹き出しを生成・管理するコンポーネント。
/// InkのTextAssetからテキストを読み取り、タイプライター表示で段階的に表示する。
/// </summary>
public class ResultSpeechBubbleController : MonoBehaviour
{
    [Header("設定")]
    /// <summary>
    /// 表示する吹き出しのプレハブ。Inspectorでドラッグ＆ドロップして設定する。
    /// </summary>
    public GameObject speechBubblePrefab;

    /// <summary>
    /// 吹き出し生成位置の基準Transform。未設定時は自身の上1.5mに生成する。
    /// </summary>
    public Transform bubbleAnchor;

    /// <summary>
    /// 1文字ごとの表示間隔（秒）。小さくすると速く表示される。
    /// </summary>
    public float typingSpeed = 0.05f;

    [Header("サウンド設定")]
    /// <summary>
    /// 文字表示時に再生する効果音。未設定の場合は効果音なしで表示する。
    /// </summary>
    public AudioClip typingSound;

    /// <summary>
    /// タイピング音の音量（0〜1）。
    /// </summary>
    [Range(0f, 1f)]
    public float typingVolume = 0.5f;

    // AudioSource参照をキャッシュ。Awakeで取得する。
    private AudioSource audioSource;
    private GameObject currentBubble;
    private UISpeechBubbleController currentBubbleController; // 型を明確化

    /// <summary>
    /// 初期化処理。UnityのAwakeでAudioSourceを取得する。AwakeはStartより先に呼ばれるため、
    /// 他のコンポーネントのStartからこのコンポーネントの参照を期待する場合に安定している。
    /// </summary>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Inkファイルからテキストを読み込み、タイプライター表示を行うシーケンスを開始する。
    /// テキスト表示後、指定時間だけ表示してから吹き出しを破棄する。
    /// </summary>
    /// <param name="inkFile">InkのTextAsset。nullなら処理を打ち切る。</param>
    /// <param name="displayDuration">テキスト全表示後の表示継続時間（秒）。</param>
    /// <returns>コルーチン用のIEnumerator。</returns>
    public IEnumerator PlaySpeechSequence(TextAsset inkFile, float displayDuration)
    {
        if (inkFile == null || speechBubblePrefab == null)
        {
            Debug.LogWarning("ResultSpeechBubble: Inkファイルまたはプレハブが設定されていません。");
            yield break;
        }

        // Ink読み込み
        Story story = new Story(inkFile.text);
        string textToDisplay = "";

        if (story.canContinue)
        {
            textToDisplay = story.Continue().Trim();
        }

        Debug.Log($"ResultSpeechBubble: 読み込んだテキスト = 「{textToDisplay}」");

        if (string.IsNullOrEmpty(textToDisplay))
        {
            Debug.LogWarning("ResultSpeechBubble: テキストが空でした。Inkファイルの中身を確認してください。");
            yield break;
        }

        // 生成
        SpawnBubble();

        // 表示開始
        yield return StartCoroutine(TypewriterRoutine(textToDisplay));

        // 待機
        yield return new WaitForSeconds(displayDuration);

        // 削除
        DestroyBubble();
    }

    /// <summary>
    /// 吹き出しプレハブを生成してコントローラーを初期化する。
    /// 既存の吹き出しがある場合は先に破棄する。
    /// </summary>
    private void SpawnBubble()
    {
        DestroyBubble();

        Vector3 spawnPos = bubbleAnchor != null ? bubbleAnchor.position : transform.position + Vector3.up * 1.5f;
        // UIの上に生成するので、親(transform)を指定してInstantiateする
        currentBubble = Instantiate(speechBubblePrefab, spawnPos, Quaternion.identity, transform);

        // コントローラーを取得
        currentBubbleController = currentBubble.GetComponent<UISpeechBubbleController>();

        if (currentBubbleController != null)
        {
            // 最初は空文字で初期化（枠だけ出す）
            currentBubbleController.ShowMessage("", 999f);
        }
        else
        {
            Debug.LogError("生成したプレハブに UISpeechBubbleController がついていません！");
        }
    }

    /// <summary>
    /// 現在の吹き出しを破棄して参照をクリアする。
    /// Nullチェックを行い、存在しない場合は何もしない。
    /// </summary>
    private void DestroyBubble()
    {
        if (currentBubble != null)
        {
            Destroy(currentBubble);
            currentBubble = null;
            currentBubbleController = null;
        }
    }

    /// <summary>
    /// 指定テキストを1文字ずつ表示するタイプライター処理。
    /// &lt;br&gt;タグを改行へ変換して表示する。
    /// </summary>
    /// <param name="fullText">表示する完全なテキスト。空文字やnullは処理を中断する。</param>
    /// <returns>コルーチン用のIEnumerator。</returns>
    private IEnumerator TypewriterRoutine(string fullText)
    {
        // コントローラーへの参照を使う（GetComponentInChildrenは使わない）
        if (currentBubbleController == null) yield break;

        fullText = fullText.Replace("<br>", "\n");

        string currentText = "";

        foreach (char letter in fullText.ToCharArray())
        {
            currentText += letter;

            currentBubbleController.SetText(currentText);

            // SE再生（AudioSourceや効果音が未設定の場合はスキップ）
            if (audioSource != null && typingSound != null)
            {
                audioSource.PlayOneShot(typingSound, typingVolume);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }
}