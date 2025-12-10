using System.Collections;
using TMPro;
using UnityEngine;

public class UISpeechBubbleController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("メッセージを表示するためのTextMeshProUGUIコンポーネント")]
    [SerializeField] private TextMeshProUGUI textMesh; // privateのまま

    private Coroutine selfDestructCoroutine;

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void ShowMessage(string message, float duration)
    {
        if (textMesh == null) return;
        textMesh.text = message;
        if (selfDestructCoroutine != null) StopCoroutine(selfDestructCoroutine);
        selfDestructCoroutine = StartCoroutine(SelfDestructRoutine(duration));
    }

    // 外部からテキストを直接セットする
    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    private IEnumerator SelfDestructRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(this.gameObject);
    }
}