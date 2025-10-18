using UnityEngine;
using UnityEngine.EventSystems;

public class InputBridge : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("参照")]
    public ScreenToWorldConverter screenToWorldConverter;
    public LayerMask draggableLayer;

    // マウス座標からDraggableを探すヘルパーメソッド
    private Draggable FindDraggable(PointerEventData eventData)
    {
        if (screenToWorldConverter.GetWorldPosition(eventData.position, out Vector3 worldPos))
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos, draggableLayer);
            if (hit != null) return hit.GetComponent<Draggable>();
        }
        return null;
    }

    // ① クリックされた瞬間の処理（選択/選択解除）
    public void OnPointerDown(PointerEventData eventData)
    {
        Draggable clickedDraggable = FindDraggable(eventData);
        DragDropManager.Instance.HandleSelectionClick(clickedDraggable, eventData);
    }

    // ② ドラッグが開始された瞬間の処理
    public void OnBeginDrag(PointerEventData eventData)
    {
        Draggable draggedDraggable = FindDraggable(eventData);
        DragDropManager.Instance.HandleBeginDrag(draggedDraggable, eventData);
    }

    // ③ ドラッグ中の処理
    public void OnDrag(PointerEventData eventData)
    {
        DragDropManager.Instance.HandleDrag(eventData);
    }

    // ④ ドラッグが終了した瞬間の処理（ドロップ）
    public void OnEndDrag(PointerEventData eventData)
    {
        DragDropManager.Instance.HandleEndDrag(eventData);
    }
}