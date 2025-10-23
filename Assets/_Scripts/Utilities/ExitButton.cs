using UnityEngine;

public class ExitButton : MonoBehaviour
{
    // ボタンのOnClick()から呼び出す
    public void QuitGame()
    {
        // エディタ上では終了しないため、エディタ時は代わりにログを出す
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
