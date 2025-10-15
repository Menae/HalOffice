using UnityEngine;

// オブジェクトにCollider2Dが必須であることを示す
[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    /// <summary>
    /// このオブジェクトがマウスでクリックされた瞬間に呼び出される
    /// </summary>
    private void OnMouseDown()
    {
        // GameManagerが存在し、入力が有効な場合のみドラッグを開始する
        if (GameManager.Instance != null && GameManager.Instance.isInputEnabled)
        {
            // 司令塔に、自分自身をドラッグ対象として通知する
            DragDropManager.Instance.StartDrag(this);
        }
    }
}