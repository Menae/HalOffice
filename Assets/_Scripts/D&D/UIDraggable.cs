using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ink.Runtime;

[RequireComponent(typeof(Image))]
public class UIDraggable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("設定")]
    [Range(1f, 3f)]
    public float dragScale = 1.2f;
    [Tooltip("このUIがどのアイテムデータに対応するかを設定")]
    public ItemData itemData;

    [Header("使用済み表現")]
    public Color usedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private Image iconImage;
    private bool isUsed = false;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
    }

    public void MarkAsUsed()
    {
        isUsed = true;
        if (iconImage != null) { iconImage.color = usedColor; }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        if (isUsed) return;
        // マネージャーに「クリック」イベントを通知する（呼び出し先も変更）
        DragDropManager.Instance.HandleItemClick(this, null, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        if (isUsed) return;
        // DragDropManagerにドラッグ開始を通知
        DragDropManager.Instance.HandleBeginDragUI(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        if (isUsed) return;
        // DragDropManagerにドラッグ中のイベントを通知
        DragDropManager.Instance.HandleDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 左クリック以外は無視

        if (isUsed) return;
        // DragDropManagerにドラッグ終了を通知
        DragDropManager.Instance.HandleEndDrag(eventData);
    }
}