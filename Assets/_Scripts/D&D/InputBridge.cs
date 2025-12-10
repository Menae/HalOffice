using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// マウス/タッチ入力を受け取り、ゲーム世界のドラッガブルとのやり取りをDragDropManagerへ委譲する。
/// MonoBehaviourのPointerイベントを利用し、UI上のドラッグとワールド上のドラッグを区別して処理する。
/// </summary>
public class InputBridge : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>
    /// Inspectorで割り当て。スクリーン座標をゲーム世界のワールド座標に変換するコンバータ。
    /// 未設定の場合はワールド位置取得に失敗し、Draggable検索が常にnullを返す。
    /// </summary>
    public ScreenToWorldConverter screenToWorldConverter;

    /// <summary>
    /// Inspectorで設定。ドラッグ対象として判定するレイヤーのマスク。
    /// </summary>
    public LayerMask draggableLayer;

    /// <summary>
    /// マウス/タッチのスクリーン位置から、対応するワールド空間のDraggableを取得する。
    /// 変換に失敗した場合や該当コライダーが見つからない場合はnullを返す。
    /// </summary>
    /// <param name="eventData">PointerEventData。positionを使用して判定。</param>
    /// <returns>見つかったDraggable、存在しない場合はnull。</returns>
    private Draggable FindDraggable(PointerEventData eventData)
    {
        if (screenToWorldConverter.GetWorldPosition(eventData.position, out Vector3 worldPos))
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos, draggableLayer);
            if (hit != null) return hit.GetComponent<Draggable>();
        }
        return null;
    }

    /// <summary>
    /// クリック（選択/選択解除）の処理を開始する。
    /// PointerClickイベントで呼ばれる。左クリック以外は無視。
    /// </summary>
    /// <param name="eventData">クリック情報。左クリック以外は処理されない。</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        Draggable clickedDraggable = FindDraggable(eventData);
        DragDropManager.Instance.HandleItemClick(null, clickedDraggable, eventData);
    }

    /// <summary>
    /// ドラッグ開始時の処理。UI上のUIDraggableによるドラッグはここでは扱わない。
    /// PointerBeginDragイベントで呼ばれる。pointerPressにUIDraggableがある場合はUIドラッグと判定して処理を行わない。
    /// </summary>
    /// <param name="eventData">ドラッグ開始情報。左クリック以外は処理されない。</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // pointerPressにUIDraggableがある場合はUIアイコンのドラッグと判断して何もしない
        if (eventData.pointerPress != null && eventData.pointerPress.GetComponent<UIDraggable>() != null)
        {
            return;
        }

        // UI上でない場合はワールド上のDraggableを探してドラッグ開始を通知
        Draggable draggedDraggable = FindDraggable(eventData);
        DragDropManager.Instance.HandleBeginDrag(draggedDraggable, eventData);
    }

    /// <summary>
    /// ドラッグ中の処理をDragDropManagerに渡す。
    /// PointerDragイベントでフレーム毎に呼ばれる。左クリック以外は無視。
    /// </summary>
    /// <param name="eventData">ドラッグ中の入力情報。</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        DragDropManager.Instance.HandleDrag(eventData);
    }

    /// <summary>
    /// ドラッグ終了（ドロップ）の処理をDragDropManagerに通知する。
    /// PointerEndDragイベントで呼ばれる。左クリック以外は無視。
    /// </summary>
    /// <param name="eventData">ドラッグ終了時の入力情報。</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        DragDropManager.Instance.HandleEndDrag(eventData);
    }
}