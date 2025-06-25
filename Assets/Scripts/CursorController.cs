using UnityEngine;

public class CursorController : MonoBehaviour
{
    // UnityエディタのInspectorから設定するための変数
    [Tooltip("カスタムカーソルの画像（Texture2D）を設定")]
    public Texture2D cursorTexture;

    [Tooltip("カーソルのクリック判定位置。左上が(0,0)")]
    public Vector2 hotspot = Vector2.zero;

    void Start()
    {
        //カーソルとして使う画像、クリック判定位置、ハードウェアカーソルかソフトウェアカーソルか自動で選択
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}