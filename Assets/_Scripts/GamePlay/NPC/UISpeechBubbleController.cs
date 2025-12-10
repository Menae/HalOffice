using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// NPCの吹き出し表示を管理する。TextMeshProUGUIにメッセージを設定し、指定時間後にゲームオブジェクトを破棄する。
/// </summary>
public class UISpeechBubbleController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("メッセージを表示するためのTextMeshProUGUIコンポーネント")]
    [SerializeField]
    // InspectorでD&D: メッセージ表示用のTextMeshProUGUIをアタッチ。未設定時はAwakeで子から検索して設定。
    private TextMeshProUGUI textMesh; // privateで管理

    private Coroutine selfDestructCoroutine;

    /// <summary>
    /// UnityのAwake。インスタンス生成直後に呼ばれ、SerializeFieldが未設定の場合は子オブジェクトからTextMeshProUGUIを検索して初期化する。
    /// Startより早い初期化を保証。
    /// </summary>
    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// メッセージを表示し、指定時間後にこのゲームオブジェクトを破棄する処理を開始する。
    /// 既に自己破壊コルーチンが動作中なら停止して上書きする。textMeshがnullなら何もしない。
    /// </summary>
    /// <param name="message">表示する文字列</param>
    /// <param name="duration">表示時間（秒）。</param>
    public void ShowMessage(string message, float duration)
    {
        if (textMesh == null) return;
        textMesh.text = message;
        if (selfDestructCoroutine != null) StopCoroutine(selfDestructCoroutine);
        selfDestructCoroutine = StartCoroutine(SelfDestructRoutine(duration));
    }

    /// <summary>
    /// 外部からテキストを直接セットする。textMeshがnullの場合は無視する。
    /// UI更新のみを担当し、表示のタイミングや破棄は呼び出し側で管理可能。
    /// </summary>
    /// <param name="text">設定するテキスト</param>
    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }

    /// <summary>
    /// 指定時間待機後にこのゲームオブジェクトを破棄するIEnumerator。
    /// WaitForSecondsを用いて非同期に待機し、その後Destroyを呼ぶ。
    /// </summary>
    /// <param name="delay">待機時間（秒）</param>
    /// <returns>破棄前までの遷移を表すIEnumerator</returns>
    private IEnumerator SelfDestructRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(this.gameObject);
    }
}