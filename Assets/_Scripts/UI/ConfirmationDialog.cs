// ファイル名: ConfirmationDialog.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // UnityActionを使用するために必要
using TMPro;

public class ConfirmationDialog : MonoBehaviour
{
    [Tooltip("ダイアログ全体の親オブジェクト")]
    public GameObject dialogPanel;
    [Tooltip("表示するメッセージのテキスト")]
    public TextMeshProUGUI messageText;
    [Tooltip("「はい」ボタン")]
    public Button yesButton;
    [Tooltip("「いいえ」ボタン")]
    public Button noButton;

    // 「はい」が押された時に実行する処理を記憶する変数
    private UnityAction onConfirmAction;

    private void Awake()
    {
        // ボタンが押された時の処理を登録
        yesButton.onClick.AddListener(OnConfirm);
        noButton.onClick.AddListener(OnCancel);
        // 最初は非表示にしておく
        dialogPanel.SetActive(false);
    }

    /// <summary>
    /// 確認ダイアログを表示する
    /// </summary>
    /// <param name="message">表示したいメッセージ</param>
    /// <param name="onConfirm">「はい」が押された時に実行したい処理</param>
    public void Show(string message, UnityAction onConfirm)
    {
        messageText.text = message;
        onConfirmAction = onConfirm;
        dialogPanel.SetActive(true);
    }

    private void OnConfirm()
    {
        // 記憶しておいた処理を実行
        onConfirmAction?.Invoke();
        dialogPanel.SetActive(false);
    }

    private void OnCancel()
    {
        dialogPanel.SetActive(false);
    }
}