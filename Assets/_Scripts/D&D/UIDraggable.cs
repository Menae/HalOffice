using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIDraggable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("アイテム設定")]
    public ItemData itemData;
    public float dragScale = 1.5f;

    [Header("ハイライト設定")]
    [Tooltip("選択時に表示する縁取りなどのハイライト用オブジェクト")]
    public GameObject highlightGraphic;

    private void Start()
    {
        SetHighlight(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        DragDropManager.Instance.HandleItemClick(this, null, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"--- UIDraggable.OnBeginDrag ---: {gameObject.name} のドラッグが検知されました。", this.gameObject);
        if (eventData.button != PointerEventData.InputButton.Left) return;
        DragDropManager.Instance.HandleBeginDragUI(this, eventData);
    }


    /// <summary>
    /// ドラッグ中に毎フレーム呼び出される
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // DragDropManagerにイベントを転送する
        DragDropManager.Instance.HandleDrag(eventData);
    }

    /// <summary>
    /// ドラッグが終了した時に呼び出される
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // DragDropManagerにイベントを転送する
        DragDropManager.Instance.HandleEndDrag(eventData);
    }

    public void MarkAsUsed()
    {
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