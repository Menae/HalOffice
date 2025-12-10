using UnityEngine;

// このスクリプトは、ScreenToWorldConverterが正しく動作しているかを
// 視覚的にテストするためだけに使用します。
public class DebugPointer : MonoBehaviour
{
    [Tooltip("目印として表示するオブジェクト")]
    public Transform pointerObject;

    void Update()
    {
        if (pointerObject == null) return;

        // ScreenToWorldConverterを使って、マウスのワールド座標を取得
        if (ScreenToWorldConverter.Instance != null &&
            ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
        {
            // 取得できた場合、目印をその座標に移動させ、表示する
            pointerObject.gameObject.SetActive(true);
            pointerObject.position = worldPos;
        }
        else
        {
            // 取得できなかった場合（カーソルがゲーム画面外にあるなど）、目印を非表示にする
            pointerObject.gameObject.SetActive(false);
        }
    }
}