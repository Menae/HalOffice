using UnityEngine;

public class CursorController : MonoBehaviour
{
    // UnityエディタのInspectorから設定するための変数
    [Tooltip("ここにカスタムカーソルの画像（Texture2D）を設定します。")]
    public Texture2D cursorTexture;

    [Tooltip("カーソルのクリック判定位置です。左上が(0,0)です。")]
    public Vector2 hotspot = Vector2.zero;

    // ゲームが開始された時に一度だけ呼ばれる関数
    void Start()
    {
        // Cursor.SetCursor() を使ってカーソルを変更します。
        // 第1引数: カーソルとして使う画像
        // 第2引数: クリック判定位置（ホットスポット）
        // 第3引数: ハードウェアカーソルかソフトウェアカーソルかを自動で選択
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}