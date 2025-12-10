using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// WebBrowserManager配下のページへのリンク機能を提供する。
/// ボタンクリック時に指定したページパネルを開く。
/// </summary>
[RequireComponent(typeof(Button))]
public class WebLinkButton : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("このリンクを押した時に開くページ（パネル）")]
    public GameObject targetPagePanel;

    [Tooltip("WebBrowserManagerの参照（自動取得できない場合のみ手動設定）")]
    public WebBrowserManager browserManager;

    private Button myButton;

    /// <summary>
    /// 初期化処理。
    /// ボタンコンポーネントの取得とクリックイベントの登録を行う。
    /// browserManagerが未設定の場合は親階層から自動取得を試みる。
    /// </summary>
    private void Start()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnLinkClicked);

        if (browserManager == null)
        {
            browserManager = GetComponentInParent<WebBrowserManager>();
        }
    }

    /// <summary>
    /// リンククリック時の処理。
    /// WebBrowserManagerを介して指定されたページパネルを開く。
    /// 必要な参照が未設定の場合は警告を出力する。
    /// </summary>
    private void OnLinkClicked()
    {
        if (browserManager != null && targetPagePanel != null)
        {
            browserManager.OpenPage(targetPagePanel);
        }
        else
        {
            Debug.LogWarning("WebLinkButton: ManagerかTargetPageが設定されていません");
        }
    }
}