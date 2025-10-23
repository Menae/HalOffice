using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UIDraggable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData itemData;
    public float dragScale = 1.5f;
    public GameObject highlightGraphic;
    private bool isUsed = false;

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
