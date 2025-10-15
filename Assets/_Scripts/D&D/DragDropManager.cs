using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }

    [Header("参照")]
    public ScreenToWorldConverter screenToWorldConverter;
    public EventSystem eventSystem;
    public Image dragProxyImage;
    public Canvas parentCanvas;

    private Draggable currentDraggedObject;
    private ObjectSlot originalSlot;
    private Vector3 originalPosition;
    private Vector2 dragOffset; // マウスカーソルとUIプロキシ中心との「差分」

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
    }

    private void Update()
    {
        if (dragProxyImage.gameObject.activeSelf)
        {
            // 現在のマウス位置をCanvasのローカル座標に変換
            Vector2 currentMouseLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                Input.mousePosition,
                parentCanvas.worldCamera,
                out currentMouseLocalPos);

            // ★★★ 修正点 ★★★
            // 新しい位置 = (現在のマウスのローカル座標) - (最初に記憶した差分)
            dragProxyImage.rectTransform.localPosition = currentMouseLocalPos - dragOffset;

            if (Input.GetMouseButtonUp(0))
            {
                HandleDrop();
            }
        }
    }

    public void StartDragFromInputBridge(PointerEventData eventData)
    {
        if (screenToWorldConverter.GetWorldPosition(eventData.position, out Vector3 worldPos))
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                Draggable draggable = hit.GetComponent<Draggable>();
                if (draggable != null)
                {
                    StartDrag(draggable, eventData.position);
                }
            }
        }
    }

    private void StartDrag(Draggable draggable, Vector2 screenPos)
    {
        if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

        currentDraggedObject = draggable;
        originalPosition = draggable.transform.position;
        originalSlot = FindObjectOfType<ObjectSlotManager>().FindSlotForDraggable(draggable);
        if (originalSlot != null)
        {
            originalSlot.currentObject = null;
        }

        // UIプロキシを準備
        SpriteRenderer sr = draggable.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            dragProxyImage.sprite = sr.sprite;
            dragProxyImage.rectTransform.sizeDelta = sr.bounds.size * 100f;
        }
        dragProxyImage.gameObject.SetActive(true);

        // ★★★ 修正点：正しいオフセット計算 ★★★
        // 1. まず、UIプロキシをクリックされた場所に一度移動させる
        Vector2 initialMouseLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos,
            parentCanvas.worldCamera,
            out initialMouseLocalPos);
        dragProxyImage.rectTransform.localPosition = initialMouseLocalPos;

        // 2. その上で、マウス位置とプロキシ中心との「差分」を計算して記憶する
        dragOffset = initialMouseLocalPos - (Vector2)dragProxyImage.rectTransform.localPosition;

        // 元のオブジェクトを非表示にする
        currentDraggedObject.gameObject.SetActive(false);
    }

    // HandleDrop, FindDropZoneUnderCursor, ReturnToOriginalSlot, PlaceInNewSlotメソッドは変更なし
    private void HandleDrop()
    {
        dragProxyImage.gameObject.SetActive(false);
        if (currentDraggedObject == null) return;
        DropZone targetZone = FindDropZoneUnderCursor();
        if (targetZone != null && targetZone.zoneType == DropZone.ZoneType.TrashCan)
        {
            Destroy(currentDraggedObject.gameObject);
        }
        else
        {
            currentDraggedObject.gameObject.SetActive(true);
            if (targetZone != null && targetZone.zoneType == DropZone.ZoneType.GameSlot && !targetZone.associatedSlot.IsOccupied())
            {
                PlaceInNewSlot(targetZone.associatedSlot);
            }
            else
            {
                ReturnToOriginalSlot();
            }
        }
        currentDraggedObject = null;
        originalSlot = null;
    }
    private DropZone FindDropZoneUnderCursor()
    {
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<InputBridge>() != null) continue;
            DropZone zone = result.gameObject.GetComponent<DropZone>();
            if (zone != null) return zone;
        }
        if (screenToWorldConverter.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
            foreach (var hit in hits)
            {
                DropZone zone = hit.GetComponent<DropZone>();
                if (zone != null) return zone;
            }
        }
        return null;
    }
    private void ReturnToOriginalSlot()
    {
        if (currentDraggedObject != null)
        {
            currentDraggedObject.transform.position = originalPosition;
            if (originalSlot != null)
            {
                originalSlot.currentObject = currentDraggedObject;
            }
        }
    }
    private void PlaceInNewSlot(ObjectSlot newSlot)
    {
        if (currentDraggedObject != null && newSlot != null)
        {
            currentDraggedObject.transform.position = newSlot.slotTransform.position;
            newSlot.currentObject = currentDraggedObject;
        }
    }
}