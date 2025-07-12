using UnityEngine;
using UnityEngine.UI;

public class SimpleCursorController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("カーソルとして表示するUIのImageコンポーネント")]
    public Image cursorImage;

    void Start()
    {
        // デフォルトのシステムカーソルを非表示にする
        Cursor.visible = false;

        // cursorImageが設定されているかチェック
        if (cursorImage == null)
        {
            Debug.LogError("Cursor Imageが設定されていません！このコンポーネントを無効にします。");
            this.enabled = false;
            return;
        }

        // カーソル画像自体がクリックイベントをブロックしないように設定
        cursorImage.raycastTarget = false;
    }

    void Update()
    {
        // 毎フレーム、UI画像の位置をマウスカーソルのスクリーン座標に合わせる
        if (cursorImage != null)
        {
            cursorImage.rectTransform.position = Input.mousePosition;
        }
    }

    // ゲームが終了、またはエディタの再生が停止した時に呼ばれる
    private void OnDestroy()
    {
        // システムカーソルを元に戻す
        Cursor.visible = true;
    }

    // このオブジェクトが無効になった時に呼ばれる
    private void OnDisable()
    {
        // システムカーソルを元に戻す
        Cursor.visible = true;
    }
}