using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DragDropManager : MonoBehaviour
{
    private enum DdState { Idle, ItemSelected, HoldingItem }
    private DdState currentState = DdState.Idle;

    [Header("参照")]
    [Tooltip("ドラッグ対象として判定する物理レイヤー")]
    public LayerMask draggableLayer;
    public ScreenToWorldConverter screenToWorldConverter;
    public EventSystem eventSystem;
    public Image dragProxyImage;
    public Canvas parentCanvas;
    public Camera mainCamera;
    public Camera uiCamera;

    // --- 内部変数 (一部変更) ---
    private Draggable currentDraggedObject;
    private Draggable selectedObject;
    private UIDraggable currentUIDraggable;
    private UIDraggable selectedUIDraggable;
    private ObjectSlot originalSlot;
    private Vector2 dragOffset;
    private bool canDrop = false;

    public static DragDropManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
    }

    public void HandleItemClick(UIDraggable uiDraggable, Draggable worldDraggable, PointerEventData eventData)
    {
        if (currentState == DdState.HoldingItem) return;

        bool isUIClick = uiDraggable != null;
        bool isWorldClick = worldDraggable != null;

        switch (currentState)
        {
            case DdState.Idle:
                if (isUIClick)
                {
                    selectedUIDraggable = uiDraggable;
                    currentState = DdState.ItemSelected;
                    if (uiDraggable.itemData?.descriptionInk != null)
                        InkDialogueManager.Instance.ShowStory(uiDraggable.itemData.descriptionInk);
                }
                else if (isWorldClick)
                {
                    selectedObject = worldDraggable;
                    currentState = DdState.ItemSelected;
                    if (worldDraggable.itemData?.descriptionInk != null)
                        InkDialogueManager.Instance.ShowStory(worldDraggable.itemData.descriptionInk);
                }
                break;

            case DdState.ItemSelected:
                // 既に選択中のUIアイテムを再度クリックした場合 -> 選択解除
                if (isUIClick && uiDraggable == selectedUIDraggable)
                {
                    currentState = DdState.Idle;
                    selectedUIDraggable = null;
                }
                // 既に選択中のWorldアイテムを再度クリックした場合 -> 選択解除
                else if (isWorldClick && worldDraggable == selectedObject)
                {
                    currentState = DdState.Idle;
                    selectedObject = null;
                }
                // 別のUIアイテムをクリックした場合 -> 選択切り替え
                else if (isUIClick)
                {
                    selectedUIDraggable = uiDraggable;
                    selectedObject = null;
                    if (uiDraggable.itemData?.descriptionInk != null)
                        InkDialogueManager.Instance.ShowStory(uiDraggable.itemData.descriptionInk);
                }
                // 別のWorldアイテムをクリックした場合 -> 選択切り替え
                else if (isWorldClick)
                {
                    selectedObject = worldDraggable;
                    selectedUIDraggable = null;
                    if (worldDraggable.itemData?.descriptionInk != null)
                        InkDialogueManager.Instance.ShowStory(worldDraggable.itemData.descriptionInk);
                }
                // 何もない場所をクリックした場合 -> 選択解除
                else
                {
                    currentState = DdState.Idle;
                    selectedObject = null;
                    selectedUIDraggable = null;
                }
                break;
        }
    }

    public void HandleBeginDrag(Draggable draggedObject, PointerEventData eventData)
    {
        if (currentState == DdState.ItemSelected && draggedObject == selectedObject)
        {
            StartHolding(draggedObject, eventData.position);
        }
    }

    public void HandleBeginDragUI(UIDraggable draggedObject, PointerEventData eventData)
    {
        if (currentState == DdState.ItemSelected && draggedObject == selectedUIDraggable)
        {
            StartHoldingUI(draggedObject, eventData);
        }
    }

    public void HandleDrag(PointerEventData eventData)
    {
        // アイテム保持中なら、代理UIの座標を更新
        if (currentState == DdState.HoldingItem)
        {
            UpdateProxyPosition(eventData.position);
        }
    }

    public void HandleEndDrag(PointerEventData eventData)
    {
        if (currentState == DdState.HoldingItem)
        {
            HandleDrop(eventData); // 引数 eventData を渡す
        }
    }

    private void StartHolding(Draggable draggable, Vector2 screenPos)
    {
        currentState = DdState.HoldingItem;
        canDrop = false;
        currentDraggedObject = draggable;
        originalSlot = draggable.currentSlot;
        if (originalSlot != null)
        {
            originalSlot.currentObject = null;
            draggable.currentSlot = null;
        }

        // 代理UIの準備
        SpriteRenderer sr = draggable.GetComponent<SpriteRenderer>();
        SetupProxy(sr.sprite);
        dragProxyImage.rectTransform.localScale = Vector3.one;
        CalculateProxySizeAndOffset(sr, draggable.transform, screenPos);
        currentDraggedObject.gameObject.SetActive(false);
    }

    private void StartHoldingUI(UIDraggable uiDraggable, PointerEventData eventData)
    {
        currentState = DdState.HoldingItem;
        canDrop = false;
        currentUIDraggable = uiDraggable;

        // 代理UIの準備
        Image sourceImage = uiDraggable.GetComponent<Image>();
        SetupProxy(sourceImage.sprite);
        dragProxyImage.rectTransform.sizeDelta = sourceImage.rectTransform.sizeDelta;
        dragProxyImage.rectTransform.localScale = Vector3.one * uiDraggable.dragScale;

        // オフセット計算
        Vector2 mouseLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, eventData.position, parentCanvas.worldCamera, out mouseLocalPos);
        Vector3 iconWorldPos = sourceImage.rectTransform.position;
        Vector2 iconLocalPos = parentCanvas.transform.InverseTransformPoint(iconWorldPos);
        dragOffset = mouseLocalPos - iconLocalPos;
    }

    private void SetupProxy(Sprite sprite)
    {
        dragProxyImage.sprite = sprite;
        dragProxyImage.gameObject.SetActive(true);

        // dragOffsetの計算は各StartDrag...メソッドに移動するため、ここでは削除

        dragProxyImage.raycastTarget = false;
    }

    private void HandleDrop(PointerEventData eventData) // 引数に eventData を追加
    {
        dragProxyImage.gameObject.SetActive(false);
        if (InkDialogueManager.Instance != null) { InkDialogueManager.Instance.CloseDialogue(); }

        // マウスを離した座標でドロップ先を探す
        DropZone targetZone = FindDropZoneUnderCursor();

        if (currentDraggedObject != null) { HandleGameWorldDrop(targetZone); }
        else if (currentUIDraggable != null) { HandleUIDrop(targetZone); }

        // 状態と変数をリセット
        currentState = DdState.Idle;
        selectedObject = null;
        selectedUIDraggable = null;
        currentDraggedObject = null;
        currentUIDraggable = null;
        originalSlot = null;
    }

    private Draggable FindDraggableAt(Vector2 screenPos)
    {
        if (screenToWorldConverter.GetWorldPosition(screenPos, out Vector3 worldPos))
        {
            // OverlapPointにレイヤーマスクを渡し、"Draggable"レイヤーのみを対象にする
            Collider2D hit = Physics2D.OverlapPoint(worldPos, draggableLayer);

            if (hit != null)
            {
                return hit.GetComponent<Draggable>();
            }
        }
        return null;
    }

    private void CalculateProxySizeAndOffset(SpriteRenderer sr, Transform draggableTransform, Vector2 screenPos)
    {
        Bounds localBounds = sr.sprite.bounds;
        Vector3[] worldCorners = new Vector3[4];
        worldCorners[0] = draggableTransform.TransformPoint(new Vector3(localBounds.min.x, localBounds.min.y, 0));
        worldCorners[1] = draggableTransform.TransformPoint(new Vector3(localBounds.min.x, localBounds.max.y, 0));
        worldCorners[2] = draggableTransform.TransformPoint(new Vector3(localBounds.max.x, localBounds.max.y, 0));
        worldCorners[3] = draggableTransform.TransformPoint(new Vector3(localBounds.max.x, localBounds.min.y, 0));

        Rect rawImageRect = screenToWorldConverter.gameScreen.rectTransform.rect;
        Vector2 localMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 localMax = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 worldPos in worldCorners)
        {
            Vector2 viewportPoint = mainCamera.WorldToViewportPoint(worldPos);
            Vector2 localPoint = new Vector2(Mathf.Lerp(rawImageRect.xMin, rawImageRect.xMax, viewportPoint.x), Mathf.Lerp(rawImageRect.yMin, rawImageRect.yMax, viewportPoint.y));
            localMin = Vector2.Min(localMin, localPoint);
            localMax = Vector2.Max(localMax, localPoint);
        }
        dragProxyImage.rectTransform.sizeDelta = localMax - localMin;

        Vector2 proxyCenterLocalPos = (localMin + localMax) / 2f;
        Vector2 mouseLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(screenToWorldConverter.gameScreen.rectTransform, screenPos, uiCamera, out mouseLocalPos);
        dragOffset = mouseLocalPos - proxyCenterLocalPos;
    }

    private void HandleGameWorldDrop(DropZone targetZone)
    {
        if (targetZone != null && targetZone.zoneType == DropZone.ZoneType.TrashCan)
        {
            // 変更前は originalSlot を保持している
            ObjectSlot removedFromSlot = originalSlot;

            Destroy(currentDraggedObject.gameObject);

            // どのスロットからオブジェクトが削除されたかを通知する
            Debug.Log($"<color=orange>Step 1: Item trashed. Firing event for slot '{removedFromSlot.slotTransform.name}'.</color>");
            GameEventManager.InvokeObjectRemoved(removedFromSlot);
        }
        else
        {
            currentDraggedObject.gameObject.SetActive(true);

            // 「スロットが空」かつ「そのスロットがこのアイテムを受け入れ可能」な場合
            if (targetZone != null &&
                targetZone.zoneType == DropZone.ZoneType.GameSlot &&
                !targetZone.associatedSlot.IsOccupied() &&
                targetZone.associatedSlot.CanAccept(currentDraggedObject.itemData.itemType))
            {
                PlaceInNewSlot(targetZone.associatedSlot);
            }
            else
            {
                ReturnToOriginalSlot();
            }
        }
    }

    private void HandleUIDrop(DropZone targetZone)
    {
        // 1. ドロップ先が見つからない、またはゲーム内スロットではない場合は、何もせず処理を終了します。
        if (targetZone == null || targetZone.zoneType != DropZone.ZoneType.GameSlot)
        {
            return;
        }

        // 2. (この時点でtargetZoneは有効) スロットのデータが正常か、かつ空いているかを確認します。
        ObjectSlot slot = targetZone.associatedSlot;
        if (slot == null || slot.IsOccupied())
        {
            return;
        }

        Draggable prefabDraggable = currentUIDraggable.itemData.itemPrefab.GetComponent<Draggable>();
        // 3. (この時点でslotは有効) ドロップするアイテムのプレハブが有効かを確認します。
        if (currentUIDraggable.itemData == null || currentUIDraggable.itemData.itemPrefab == null) // itemData経由でチェック
        {
            Debug.LogError($"UIアイテム '{currentUIDraggable.name}' のItemDataまたはPrefabが正しく設定されていません。", currentUIDraggable);
            return;
        }

        // 全てのチェックを通過した場合のみ、アイテムを生成して配置します。
        GameObject newItem = Instantiate(
            currentUIDraggable.itemData.itemPrefab, // itemData経由で参照
            slot.slotTransform.position,
            Quaternion.identity
        );

        Draggable newDraggable = newItem.GetComponent<Draggable>();
        if (newDraggable != null)
        {
            newDraggable.itemData = currentUIDraggable.itemData; // ◀◀◀ 【重要】この行を追加！
            slot.currentObject = newDraggable;
            newDraggable.currentSlot = slot;

            currentUIDraggable.MarkAsUsed();

            // ObjectSlotManagerに、このスロットが更新されたことを通知する
            FindObjectOfType<ObjectSlotManager>().MarkSlotAsNewlyPlaced(slot);
        }
    }

    private bool IsDraggable() { return (GameManager.Instance == null || GameManager.Instance.isInputEnabled); }
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
            // originalPositionではなく、スロットの位置に戻す
            if (originalSlot != null)
            {
                currentDraggedObject.transform.position = originalSlot.slotTransform.position;
                originalSlot.currentObject = currentDraggedObject;
                currentDraggedObject.currentSlot = originalSlot;
            }
            // もしスロットに属さないオブジェクトだった場合のフォールバック（必要なら）
            // else { currentDraggedObject.transform.position = originalPosition; }
        }
    }

    private void PlaceInNewSlot(ObjectSlot newSlot)
    {
        if (currentDraggedObject != null && newSlot != null)
        {
            currentDraggedObject.transform.position = newSlot.slotTransform.position;
            newSlot.currentObject = currentDraggedObject;

            currentDraggedObject.currentSlot = newSlot;
        }
    }

    private void UpdateProxyPosition(Vector2 screenPos)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, screenPos, parentCanvas.worldCamera, out localPoint);
        dragProxyImage.rectTransform.localPosition = localPoint - dragOffset;
    }
}