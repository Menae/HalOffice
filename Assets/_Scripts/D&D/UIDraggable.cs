using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Ink.Runtime;

[RequireComponent(typeof(Image))]
public class UIDraggable : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("設定")]
    public GameObject itemPrefab;
    [Range(1f, 3f)]
    public float dragScale = 1.2f;
    [Tooltip("このアイテムを選択した時に表示する会話（InkのJSONファイル）")]
    public TextAsset descriptionInk;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isUsed) return;
        // DragDropManagerにUIアイテムがクリックされたことを通知
        DragDropManager.Instance.HandleClickOnUI(this, eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUsed) return;
        // DragDropManagerにドラッグ開始を通知
        DragDropManager.Instance.HandleBeginDragUI(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isUsed) return;
        // DragDropManagerにドラッグ中のイベントを通知
        DragDropManager.Instance.HandleDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isUsed) return;
        // DragDropManagerにドラッグ終了を通知
        DragDropManager.Instance.HandleEndDrag(eventData);
    }
}