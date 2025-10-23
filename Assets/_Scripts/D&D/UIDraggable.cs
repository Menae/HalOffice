using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIDraggable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler
{
    [Header("アイテム設定")]
    public ItemData itemData;
    public float dragScale = 1.5f;

    [Header("ハイライト設定")]
    [Tooltip("選択時に表示する縁取りなどのハイライト用オブジェクト")]
    public GameObject highlightGraphic;

    private void Start()
    {
        // ゲーム開始時は、必ずハイライトを非表示にしておく
        SetHighlight(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        DragDropManager.Instance.HandleItemClick(this, null, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        DragDropManager.Instance.HandleBeginDragUI(this, eventData);
    }

    public void MarkAsUsed()
    {
        GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        GetComponent<Button>().interactable = false;
    }

    /// <summary>
    /// ハイライトの表示/非表示を切り替える
    /// </summary>
    public void SetHighlight(bool isActive)
    {
        if (highlightGraphic != null)
        {
            highlightGraphic.SetActive(isActive);
        }
    }
}