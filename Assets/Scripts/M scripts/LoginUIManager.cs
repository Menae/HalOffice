using UnityEngine;

public class LoginUIManager : MonoBehaviour
{
    public GameObject SignInPanel;
    public GameObject PasswordPanel;
    public void OnTitleClick()
    {
        SignInPanel.SetActive(false);
        PasswordPanel.SetActive(true);
    }

    public void OnExitClick()
    {
        Application.Quit();
    }

}