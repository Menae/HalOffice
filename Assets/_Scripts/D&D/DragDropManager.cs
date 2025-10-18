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

    void Update()
    {

    }

    public void HandleSelectionClick(Draggable clickedDraggable, PointerEventData eventData)
    {
        // アイテム保持中は選択処理を受け付けない
        if (currentState == DdState.HoldingItem) return;

        // ▼▼▼ 以下のswitch文の case DdState.ItemSelected: を修正 ▼▼▼
        switch (currentState)
        {
            case DdState.Idle:
                if (clickedDraggable != null)
                {
                    selectedObject = clickedDraggable;
                    selectedUIDraggable = null;
                    currentState = DdState.ItemSelected;
                    Debug.Log($"{selectedObject.name} を選択。");

                    if (selectedObject.descriptionInk != null)
                    {
                        InkDialogueManager.Instance.ShowStory(selectedObject.descriptionInk);
                    }
                }
                break;

            case DdState.ItemSelected:
                // 別のオブジェクトをクリックしたら選択を切り替える
                if (clickedDraggable != null && clickedDraggable != selectedObject)
                {
                    selectedObject = clickedDraggable;
                    selectedUIDraggable = null;
                    Debug.Log($"{selectedObject.name} に選択を切り替え。");

                    if (selectedObject.descriptionInk != null)
                    {
                        InkDialogueManager.Instance.ShowStory(selectedObject.descriptionInk);
                    }
                }
                // 何もない場所をクリックしたら選択を解除する
                else if (clickedDraggable == null)
                {
                    selectedObject = null;
                    selectedUIDraggable = null;
                    currentState = DdState.Idle;
                    Debug.Log("選択を解除。");
                }
                // 選択中のオブジェクトを再度クリックした場合は何もしない
                // （ドラッグ開始はOnBeginDragが検知してHandleBeginDragを呼び出す）
                break;
        }
    }

    public void HandleBeginDrag(Draggable draggedObject, PointerEventData eventData)
    {
        // 「選択中」の状態で、かつ「選択されているオブジェクト」上でドラッグが開始された場合のみ処理
        if (currentState == DdState.ItemSelected && draggedObject == selectedObject)
        {
            StartHolding(draggedObject, eventData.position);
        }
    }

    public void HandleBeginDragUI(UIDraggable draggedObject, PointerEventData eventData)
    {
        // 「選択中」の状態で、かつ「選択されているUIオブジェクト」上でドラッグが開始された場合のみ処理
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

    public void HandleClickOnUI(UIDraggable clickedUIDraggable, PointerEventData eventData)
    {
        if (currentState == DdState.HoldingItem)
        {
            HandleDrop(eventData);
            return;
        }

        switch (currentState)
        {
            case DdState.Idle:
                if (clickedUIDraggable != null)
                {
                    selectedUIDraggable = clickedUIDraggable;
                    selectedObject = null; // 他方の選択を解除
                    currentState = DdState.ItemSelected;
                    Debug.Log($"{selectedUIDraggable.name} を選択しました。");

                    if (selectedUIDraggable.descriptionInk != null)
                    {
                        InkDialogueManager.Instance.ShowStory(selectedUIDraggable.descriptionInk);
                    }
                }
                break;

            case DdState.ItemSelected:
                if (clickedUIDraggable == selectedUIDraggable)
                {
                    StartHoldingUI(selectedUIDraggable, eventData);
                    UpdateProxyPosition(eventData.position);
                }
                else if (clickedUIDraggable != null)
                {
                    selectedUIDraggable = clickedUIDraggable;
                    selectedObject = null; // 他方の選択を解除
                    Debug.Log($"{selectedUIDraggable.name} に選択を切り替えました。");

                    if (selectedUIDraggable.descriptionInk != null)
                    {
                        InkDialogueManager.Instance.ShowStory(selectedUIDraggable.descriptionInk);
                    }
                }
                else
                {
                    selectedObject = null;
                    selectedUIDraggable = null; // 他方の選択を解除
                    currentState = DdState.Idle;
                    Debug.Log("選択を解除しました。");
                }
                break;
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
            Destroy(currentDraggedObject.gameObject);
        }
        else
        {
            currentDraggedObject.gameObject.SetActive(true);

            // 「スロットが空」かつ「そのスロットがこのアイテムを受け入れ可能」な場合
            if (targetZone != null &&
                targetZone.zoneType == DropZone.ZoneType.GameSlot &&
                !targetZone.associatedSlot.IsOccupied() &&
                targetZone.associatedSlot.CanAccept(currentDraggedObject.itemType)) // ← このチェックを追加
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
        // --- ガード節1: ドロップ先が無効な場所なら、何もせずに終了 ---
        if (targetZone == null || targetZone.zoneType != DropZone.ZoneType.GameSlot)
        {
            // オプション：もしUIアイテムを元の場所に戻すなどの演出が必要ならここに追加
            return;
        }

        // --- ガード節2: ドロップ先のスロットが既に埋まっているなら、何もせずに終了 ---
        if (targetZone.associatedSlot.IsOccupied())
        {
            return;
        }

        // --- ガード節3: UIアイテムのプレハブが正しく設定されていないなら、エラーを出して終了 ---
        Draggable prefabDraggable = currentUIDraggable.itemPrefab.GetComponent<Draggable>();
        if (prefabDraggable == null)
        {
            Debug.LogError($"UIアイテム '{currentUIDraggable.name}' のプレハブにDraggableコンポーネントがありません。", currentUIDraggable);
            return;
        }

        // 全てのチェックを通過した場合のみ、アイテムを生成して配置する
        GameObject newItem = Instantiate(
            currentUIDraggable.itemPrefab,
            targetZone.associatedSlot.slotTransform.position,
            Quaternion.identity
        );

        Draggable newDraggable = newItem.GetComponent<Draggable>();
        if (newDraggable != null)
        {
            targetZone.associatedSlot.currentObject = newDraggable;
            newDraggable.currentSlot = targetZone.associatedSlot;

            // ドロップに成功したので、元のUIアイテムに使用済みになったことを通知する
            currentUIDraggable.MarkAsUsed();
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