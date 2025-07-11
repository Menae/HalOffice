using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
   
    public void OnSearchButtonClick()
    {
        SceneManager.LoadScene("P3_stealth");
    }
}


