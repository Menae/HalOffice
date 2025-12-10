using TMPro;
using UnityEngine;
using UnityEngine.Events; // UnityActionを使用するために必要
using UnityEngine.UI;

/// <summary>
/// 確認ダイアログを管理するコンポーネント。メッセージ表示と「はい」「いいえ」での処理を制御する。
/// Awakeでボタンリスナーを登録し、Showで表示とコールバックを設定する。
/// </summary>
public class ConfirmationDialog : MonoBehaviour
{
    [Tooltip("ダイアログ全体の親オブジェクト")]
    public GameObject dialogPanel; // Inspectorで参照を設定。非表示でダイアログを閉じる。

    [Tooltip("表示するメッセージのテキスト")]
    public TextMeshProUGUI messageText; // InspectorでD&D。未設定だとNullReferenceExceptionになるため設定必須。

    [Tooltip("「はい」ボタン")]
    public Button yesButton; // Inspectorでボタンをアサイン。Awakeでリスナー登録。

    [Tooltip("「いいえ」ボタン")]
    public Button noButton; // Inspectorでボタンをアサイン。Awakeでリスナー登録。

    public Button closeButton; // Inspectorでクローズ用のボタンをアサイン。Awakeでリスナー登録。

    // 「はい」が押された時に実行する処理を記憶する変数
    private UnityAction onConfirmAction;

    /// <summary>
    /// UnityのAwakeイベント。オブジェクト生成時に呼ばれる（Startより先に実行）。
    /// ボタンのクリックリスナーを登録し、初期状態でダイアログを非表示にする。
    /// </summary>
    private void Awake()
    {
        // ボタンが押された時の処理を登録
        yesButton.onClick.AddListener(OnConfirm);
        noButton.onClick.AddListener(OnCancel);
        closeButton.onClick.AddListener(OnCancel);

        // 最初は非表示にしておく
        dialogPanel.SetActive(false);
    }

    /// <summary>
    /// 確認ダイアログを表示する。messageを表示し、onConfirmを「はい」押下時に呼び出す。
    /// onConfirmはnull可で、その場合は単にダイアログを閉じる。
    /// </summary>
    /// <param name="message">表示したいメッセージ。空文字も許容。</param>
    /// <param name="onConfirm">「はい」が押された時に実行したい処理。null可。</param>
    public void Show(string message, UnityAction onConfirm)
    {
        messageText.text = message;
        onConfirmAction = onConfirm;
        dialogPanel.SetActive(true);
    }

    /// <summary>
    /// 「はい」ボタン押下時の処理。保存されたonConfirmActionを呼び出し、ダイアログを閉じる。
    /// onConfirmActionがnullなら何も呼ばれずダイアログのみ閉じる。
    /// </summary>
    private void OnConfirm()
    {
        // 記憶しておいた処理を実行
        onConfirmAction?.Invoke();
        dialogPanel.SetActive(false);
    }

    /// <summary>
    /// 「いいえ」またはクローズボタン押下時の処理。ダイアログを閉じる。
    /// </summary>
    private void OnCancel()
    {
        dialogPanel.SetActive(false);
    }
}