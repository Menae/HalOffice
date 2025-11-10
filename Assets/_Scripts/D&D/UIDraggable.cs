using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UIDraggable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData itemData;
    public float dragScale = 1.5f;
    public GameObject highlightGraphic;

    private bool isUsed = false;
    private ScrollRect parentScrollRect;
    private CanvasGroup canvasGroup;


    private void Start()
    {
        SetHighlight(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUsed) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        DragDropManager.Instance.HandleItemClick(this, null, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUsed) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 親の ScrollRect を一時的に無効化（ドラッグとスクロールの競合回避）
        if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
        if (parentScrollRect != null) parentScrollRect.enabled = false;

        // ドロップ先のUI/ワールドへレイキャストを通すためにブロック解除（DragDropManagerの実装に合わせて）
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        DragDropManager.Instance.HandleBeginDragUI(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isUsed) return;
        DragDropManager.Instance.HandleDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isUsed) return;

        DragDropManager.Instance.HandleEndDrag(eventData);

        // 復帰
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        if (parentScrollRect != null) parentScrollRect.enabled = true;
    }

    public void MarkAsUsed()
    {
        isUsed = true;
        GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        if (GetComponent<Button>() != null)
        {
            GetComponent<Button>().interactable = false;
        }
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightGraphic != null)
        {
            highlightGraphic.SetActive(isActive);
        }
    }
}
