using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoginUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SignInPanel;
    public GameObject DesktopPanel;
    public GameObject tuuchImage;
    public GameObject TuchiPanel;
    public Button tabCloseButton; 

    [Header("Audio / Cursor")]
    public AudioClip tenkai03;
    public AudioClip tuuchiKari;
    public AudioSource audioSource;
    public Texture2D arrowCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    private void Start()
    {
        // カーソル変更
        Cursor.SetCursor(arrowCursor, cursorHotspot, CursorMode.Auto);

        // 初期表示
        SignInPanel?.SetActive(true);
        DesktopPanel?.SetActive(false);
        tuuchImage?.SetActive(false);
        TuchiPanel?.SetActive(false);
        if (tabCloseButton != null)
        {
            tabCloseButton.interactable = false;
        }
    }

    public void OnStartClick()
    {
        SignInPanel?.SetActive(false);
        DesktopPanel?.SetActive(true);

        // 音再生
        if (tenkai03 != null)
        {
            if (audioSource != null)
            {
                audioSource.clip = tenkai03;
                audioSource.Play();
            }
            else
            {
                AudioSource.PlayClipAtPoint(tenkai03, Camera.main.transform.position);
            }
        }

        // 5秒後に画像表示＋tuuchiKari再生
        StartCoroutine(ShowAfterDelay(tuuchImage, 3f));
    }

    public void OnExitClick()
    {
        Application.Quit();
    }

    public void OnBellButtonClick()
    {
        if (TuchiPanel != null)
        {
            TuchiPanel.SetActive(true);
        }
        if (tuuchImage != null)
        {
            tuuchImage.SetActive(false);
        }
        if (tabCloseButton != null)
        {
            tabCloseButton.interactable = true;
        }
    }

    public void OnTabCloseButtonClick()
    {
        if (TuchiPanel != null)
        {
            TuchiPanel.SetActive(false);
        }
    }

    private IEnumerator ShowAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj?.SetActive(true);
        // tuuchiKariを再生
        if (tuuchiKari != null)
        {
            if (audioSource != null)
            {
                audioSource.clip = tuuchiKari;
                audioSource.Play();
            }
            else
            {
                AudioSource.PlayClipAtPoint(tuuchiKari, Camera.main.transform.position);
            }
        }
    }
}
