using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebBrowserManager : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("一番最初に表示される一覧画面（ホーム）")]
    public GameObject homePanel;

    [Tooltip("全画面共通の「戻るボタン」")]
    public Button backButton;

    [Header("管理")]
    [Tooltip("制御下にある全てのページ（ホーム含む）。これらを自動で非表示にします。")]
    public List<GameObject> allPages;

    private GameObject currentPage;

    private void Start()
    {
        // 戻るボタンに機能を登録
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoHome);
        }

        // 最初はホームを表示
        GoHome();
    }

    /// <summary>
    /// 指定されたページパネルを開く
    /// </summary>
    public void OpenPage(GameObject pageToOpen)
    {
        // 全ページを非表示にする
        HideAllPages();

        // 指定されたページだけ表示
        if (pageToOpen != null)
        {
            pageToOpen.SetActive(true);
            currentPage = pageToOpen;
        }

        // ホーム以外にいるなら「戻るボタン」を表示
        if (backButton != null)
        {
            bool isHome = (pageToOpen == homePanel);
            backButton.gameObject.SetActive(!isHome);
        }
    }

    /// <summary>
    /// ホーム画面に戻る
    /// </summary>
    public void GoHome()
    {
        OpenPage(homePanel);
    }

    private void HideAllPages()
    {
        foreach (var page in allPages)
        {
            if (page != null) page.SetActive(false);
        }
    }
}