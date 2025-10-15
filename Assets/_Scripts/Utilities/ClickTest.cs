using UnityEngine;

// このスクリプトは、クリックを検知してログを出すためだけに存在する
public class ClickTest : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log(gameObject.name + "がクリックされました！ OnMouseDown()が動作しています。");
    }
}