using UnityEngine;
using System.Collections;
using TMPro;
using Ink.Runtime;

[RequireComponent(typeof(AudioSource))]
public class ResultSpeechBubbleController : MonoBehaviour
{
    [Header("設定")]
    public GameObject speechBubblePrefab;
    public Transform bubbleAnchor;
    public float typingSpeed = 0.05f;

    [Header("サウンド設定")]
    public AudioClip typingSound;
    [Range(0f, 1f)]
    public float typingVolume = 0.5f;

    private AudioSource audioSource;
    private GameObject currentBubble;
    private UISpeechBubbleController currentBubbleController; // 型を明確化

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

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

        // ▼▼▼ デバッグログ: 何が読み込まれたか確認 ▼▼▼
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

    private void DestroyBubble()
    {
        if (currentBubble != null)
        {
            Destroy(currentBubble);
            currentBubble = null;
            currentBubbleController = null;
        }
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        // コントローラーへの参照を使う（GetComponentInChildrenはもう使わない）
        if (currentBubbleController == null) yield break;

        string currentText = "";

        foreach (char letter in fullText.ToCharArray())
        {
            currentText += letter;

            // ▼▼▼ 追加したSetTextメソッドを使って安全に更新 ▼▼▼
            currentBubbleController.SetText(currentText);

            // SE再生
            if (audioSource != null && typingSound != null)
            {
                audioSource.PlayOneShot(typingSound, typingVolume);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }
}