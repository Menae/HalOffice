using UnityEngine;

public class LoginUIManager : MonoBehaviour
{
    public GameObject SignInPanel;
    public GameObject DesktopPanel;
    public AudioClip tenkai03;
 
    public Texture2D arrowCursor;
    public Vector2 cursorHotspot = Vector2.zero;

    void Start()
    {
        // Arrow2画像でカーソルを変更
        if (arrowCursor != null)
        {
            Cursor.SetCursor(arrowCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    public void OnStartClick()
    {
        SignInPanel.SetActive(false);
        DesktopPanel.SetActive(true);
        // tenkai03を再生
        if (tenkai03 != null)
        {
            AudioSource.PlayClipAtPoint(tenkai03, Camera.main.transform.position);
        }
    }

    public void OnExitClick()
    {
        Application.Quit();
    }
}