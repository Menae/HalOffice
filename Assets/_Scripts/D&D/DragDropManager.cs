using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DragDropManager : MonoBehaviour
{
    /// <summary>
    /// 現在アクティブなCursorControllerを保持する静的プロパティ
    /// </summary>
    public static CursorController ActiveCursor { get; private set; }

    /// <summary>
    /// CursorControllerが自分自身を登録するためのメソッド
    /// </summary>
    public static void RegisterCursor(CursorController cursor)
    {
        ActiveCursor = cursor;
    }

    /// <summary>
    /// CursorControllerが自分自身を登録解除するためのメソッド
    /// </summary>
    public static void UnregisterCursor(CursorController cursor)
    {
        // 念のため、現在登録されているのが自分自身である場合のみ解除
        if (ActiveCursor == cursor)
        {
            ActiveCursor = null;
        }
    }

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
        currentDraggedObject = draggable;
        originalSlot = draggable.currentSlot;
        if (originalSlot != null)
        {
            originalSlot.currentObject = null;
            draggable.currentSlot = null;
        }

        SpriteRenderer sr = draggable.GetComponent<SpriteRenderer>();
        SetupProxy(sr.sprite);


        // SpriteRendererのワールド空間での境界を取得
        Bounds worldBounds = sr.bounds;

        // 境界の最小と最大の角をスクリーン座標に変換
        Vector2 minScreenPoint = mainCamera.WorldToScreenPoint(worldBounds.min);
        Vector2 maxScreenPoint = mainCamera.WorldToScreenPoint(worldBounds.max);

        // スクリーン座標での差分から、ピクセル単位での大きさを計算
        Vector2 pixelSize = maxScreenPoint - minScreenPoint;

        // 計算した大きさを代理UIのsizeDeltaに設定
        dragProxyImage.rectTransform.sizeDelta = new Vector2(Mathf.Abs(pixelSize.x), Mathf.Abs(pixelSize.y));
        dragProxyImage.rectTransform.localScale = Vector3.one;


        if (ActiveCursor != null)
        {
            dragProxyImage.transform.SetParent(ActiveCursor.cursorImage.transform, true);
            dragProxyImage.rectTransform.anchoredPosition = Vector2.zero;
        }

        currentDraggedObject.gameObject.SetActive(false);
    }

    private void StartHoldingUI(UIDraggable uiDraggable, PointerEventData eventData)
    {
        currentState = DdState.HoldingItem;
        currentUIDraggable = uiDraggable;

        Image sourceImage = uiDraggable.GetComponent<Image>();
        SetupProxy(sourceImage.sprite);
        dragProxyImage.rectTransform.sizeDelta = sourceImage.rectTransform.sizeDelta;
        dragProxyImage.rectTransform.localScale = Vector3.one * uiDraggable.dragScale;

        if (ActiveCursor != null)
        {
            dragProxyImage.transform.SetParent(ActiveCursor.cursorImage.transform, true);
            dragProxyImage.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private void SetupProxy(Sprite sprite)
    {
        dragProxyImage.sprite = sprite;
        dragProxyImage.gameObject.SetActive(true);

        // dragOffsetの計算は各StartDrag...メソッドに移動するため、ここでは削除

        dragProxyImage.raycastTarget = false;
    }

    private void HandleDrop(PointerEventData eventData)
    {
        dragProxyImage.transform.SetParent(parentCanvas.transform, true);

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
}