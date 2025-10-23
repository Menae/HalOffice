using UnityEngine;
using UnityEngine.EventSystems;

public class InputBridge : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        Draggable clickedDraggable = FindDraggable(eventData);
        // マネージャーに「クリック」イベントを通知する（呼び出し先も変更）
        DragDropManager.Instance.HandleItemClick(null, clickedDraggable, eventData);
    }

    // ② ドラッグが開始された瞬間の処理
public void OnBeginDrag(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;

    // マウスが最初に押されたオブジェクト（pointerPress）にUIDraggableがあるかチェック
    if (eventData.pointerPress != null && eventData.pointerPress.GetComponent<UIDraggable>() != null)
    {
        // あった場合、それはUIアイコンのドラッグなので、InputBridgeは何もしない
        return;
    }

    // UIアイコン上でなければ、通常通りゲーム世界のオブジェクトを探してドラッグを開始する
    Draggable draggedDraggable = FindDraggable(eventData);
    DragDropManager.Instance.HandleBeginDrag(draggedDraggable, eventData);
}

    // ③ ドラッグ中の処理
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        DragDropManager.Instance.HandleDrag(eventData);
    }

    // ④ ドラッグが終了した瞬間の処理（ドロップ）
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        DragDropManager.Instance.HandleEndDrag(eventData);
    }
}