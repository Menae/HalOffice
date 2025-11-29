using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WebLinkButton : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("このリンクを押した時に開くページ（パネル）")]
    public GameObject targetPagePanel;

    [Tooltip("WebBrowserManagerの参照（自動取得できない場合のみ手動設定）")]
    public WebBrowserManager browserManager;

    private Button myButton;

    private void Start()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnLinkClicked);

        // マネージャーが未設定なら親から探す
        if (browserManager == null)
        {
            browserManager = GetComponentInParent<WebBrowserManager>();
        }
    }

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