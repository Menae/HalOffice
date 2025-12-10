using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Webブラウザ風のページ遷移を管理する。
/// ホーム画面を起点に、各ページの表示/非表示と戻るボタンの制御を行う。
/// </summary>
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

    /// <summary>
    /// 現在表示中のページを保持。
    /// </summary>
    private GameObject currentPage;

    /// <summary>
    /// 初期化処理。
    /// 戻るボタンにGoHomeを登録し、ホーム画面を表示する。
    /// </summary>
    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoHome);
        }

        GoHome();
    }

    /// <summary>
    /// 指定されたページパネルを開く。
    /// 他の全ページを非表示にし、ホーム以外では戻るボタンを表示する。
    /// </summary>
    /// <param name="pageToOpen">表示するページのGameObject</param>
    public void OpenPage(GameObject pageToOpen)
    {
        HideAllPages();

        if (pageToOpen != null)
        {
            pageToOpen.SetActive(true);
            currentPage = pageToOpen;
        }

        if (backButton != null)
        {
            bool isHome = (pageToOpen == homePanel);
            backButton.gameObject.SetActive(!isHome);
        }
    }

    /// <summary>
    /// ホーム画面に戻る。
    /// OpenPageを介してホームパネルを表示し、戻るボタンを非表示にする。
    /// </summary>
    public void GoHome()
    {
        OpenPage(homePanel);
    }

    /// <summary>
    /// allPagesに登録された全ページを非表示にする。
    /// nullチェックを行い、安全に非アクティブ化する。
    /// </summary>
    private void HideAllPages()
    {
        foreach (var page in allPages)
        {
            if (page != null) page.SetActive(false);
        }
    }
}