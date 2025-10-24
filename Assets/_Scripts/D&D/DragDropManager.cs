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

    [Header("サウンド設定")]
    [Tooltip("効果音を再生するためのAudioSource")]
    public AudioSource audioSource;
    [Tooltip("アイテムをゴミ箱に捨てた時の効果音")]
    public AudioClip trashSound;
    [Range(0f, 1f)]
    [Tooltip("ゴミ箱の効果音の音量")]
    public float trashVolume = 1.0f;

    [Header("配置SE設定")]
    [Tooltip("アイテムを正解のスロットに配置した時の効果音")]
    public AudioClip correctPlacementSound;
    [Range(0f, 1f)]
    [Tooltip("正解の効果音の音量")]
    public float correctPlacementVolume = 1.0f;
    [Tooltip("アイテムを不正解のスロットに配置した時の効果音")]
    public AudioClip incorrectPlacementSound;
    [Range(0f, 1f)]
    [Tooltip("不正解の効果音の音量")]
    public float incorrectPlacementVolume = 1.0f;

    // --- 内部変数 (一部変更) ---
    private Draggable currentDraggedObject;
    private Draggable selectedObject;
    private UIDraggable currentUIDraggable;
    private UIDraggable selectedUIDraggable;
    private ObjectSlot originalSlot;
    private bool canDrop = false;

    public static DragDropManager Instance { get; private set; }

    public bool InteractionEnabled { get; private set; } = true;

    public void SetInteractionEnabled(bool enabled)
    {
        InteractionEnabled = enabled;
    }


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
    }

    public void HandleItemClick(UIDraggable uiDraggable, Draggable worldDraggable, PointerEventData eventData)
    {
        if (!InteractionEnabled) return;
        if (currentState == DdState.HoldingItem) return;

        object clickedItem = uiDraggable != null ? (object)uiDraggable : (object)worldDraggable;
        object previouslySelectedItem = selectedUIDraggable != null ? (object)selectedUIDraggable : (object)selectedObject;

        // --- 選択解除 ---
        // 以前に選択されていたものがあれば、ハイライトを消す
        if (previouslySelectedItem != null)
        {
            if (selectedUIDraggable != null) selectedUIDraggable.SetHighlight(false);
            if (selectedObject != null) selectedObject.SetHighlight(false);
        }

        // --- 選択処理 ---
        // 何もクリックされなかったか、同じアイテムを再度クリックした場合は、選択解除してIdle状態にする
        if (clickedItem == null || clickedItem == previouslySelectedItem)
        {
            selectedUIDraggable = null;
            selectedObject = null;
            currentState = DdState.Idle;
        }
        // 違うアイテムがクリックされた場合は、選択を切り替える
        else
        {
            if (uiDraggable != null)
            {
                selectedUIDraggable = uiDraggable;
                selectedObject = null; // ワールドオブジェクトの選択は解除
                selectedUIDraggable.SetHighlight(true);
                // 既存のダイアログ表示ロジックを維持
                if (selectedUIDraggable.itemData?.descriptionInk != null)
                    InkDialogueManager.Instance.ShowStory(selectedUIDraggable.itemData.descriptionInk);
            }
            else // worldDraggable != null
            {
                selectedObject = worldDraggable;
                selectedUIDraggable = null; // UIオブジェクトの選択は解除
                selectedObject.SetHighlight(true);
                // 既存のダイアログ表示ロジックを維持
                if (selectedObject.itemData?.descriptionInk != null)
                    InkDialogueManager.Instance.ShowStory(selectedObject.itemData.descriptionInk);
            }
            currentState = DdState.ItemSelected;
        }
    }

    public void HandleBeginDrag(Draggable draggedObject, PointerEventData eventData)
    {
        if (!InteractionEnabled) return;

        // ケース1: アイテムが既に選択されている状態で、ドラッグが開始された場合
        if (currentState == DdState.ItemSelected && draggedObject == selectedObject)
        {
            StartHolding(draggedObject, eventData.position);
        }
        // ケース2: 未選択状態(Idle)から、いきなりドラッグが開始された場合
        else if (currentState == DdState.Idle && draggedObject != null)
        {
            // その場でアイテムを選択状態にしてから、即座にドラッグを開始する
            selectedObject = draggedObject;
            selectedObject.SetHighlight(true);
            currentState = DdState.ItemSelected;
            StartHolding(draggedObject, eventData.position);
        }
    }

    public void HandleBeginDragUI(UIDraggable draggedObject, PointerEventData eventData)
    {
        Debug.Log($"--- DragDropManager.HandleBeginDragUI ---: 呼び出されました。");
        Debug.Log($"現在の状態(currentState): {currentState}");
        Debug.Log($"選択中のUIオブジェクト(selectedUIDraggable): {(selectedUIDraggable != null ? selectedUIDraggable.name : "null")}");
        Debug.Log($"ドラッグされたオブジェクト(draggedObject): {(draggedObject != null ? draggedObject.name : "null")}");

        if (!InteractionEnabled) return;

        // ケース1：アイテムが既に選択されている状態で、ドラッグが開始された場合
        if (currentState == DdState.ItemSelected && draggedObject == selectedUIDraggable)
        {
            Debug.Log("条件成功：ドラッグを開始します。");
            StartHoldingUI(draggedObject, eventData);
        }
        // ケース2：未選択状態(Idle)から、いきなりドラッグが開始された場合
        else if (currentState == DdState.Idle && draggedObject != null)
        {
            Debug.Log("条件成功（Idleから）：選択してドラッグを開始します。");
            selectedUIDraggable = draggedObject;
            selectedUIDraggable.SetHighlight(true);
            currentState = DdState.ItemSelected;
            StartHoldingUI(draggedObject, eventData);
        }
        else
        {
            Debug.LogError("条件失敗：ドラッグを開始できませんでした。");
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
    // --- 代理イメージの後処理 ---
    dragProxyImage.transform.SetParent(parentCanvas.transform, true);
    dragProxyImage.gameObject.SetActive(false);
    if (InkDialogueManager.Instance != null) { InkDialogueManager.Instance.CloseDialogue(); }

    // --- ドロップ先の判定 ---
    DropZone targetZone = FindDropZoneUnderCursor();

    // --- オブジェクトごとのドロップ処理 ---
    if (currentDraggedObject != null)
    {
        HandleGameWorldDrop(targetZone);
        currentDraggedObject.SetHighlight(false); // ★ハイライトを消す
    }
    else if (currentUIDraggable != null)
    {
        HandleUIDrop(targetZone);
        currentUIDraggable.SetHighlight(false); // ★ハイライトを消す
    }

    // --- 状態のリセット ---
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
            if (audioSource != null && trashSound != null)
            {
                audioSource.PlayOneShot(trashSound, trashVolume);
            }

            ObjectSlot removedFromSlot = originalSlot;
            Destroy(currentDraggedObject.gameObject);
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
                // 正解・不正解を判定してSEを再生
                ObjectSlot slot = targetZone.associatedSlot;
                // このスロットの正解タイプと、置いたアイテムのタイプが一致するか？
                if (slot.correctItemType == currentDraggedObject.itemData.itemType)
                {
                    // 正解のSEを再生
                    if (audioSource != null && correctPlacementSound != null)
                    {
                        audioSource.PlayOneShot(correctPlacementSound, correctPlacementVolume);
                    }
                }
                else
                {
                    // 不正解のSEを再生
                    if (audioSource != null && incorrectPlacementSound != null)
                    {
                        audioSource.PlayOneShot(incorrectPlacementSound, incorrectPlacementVolume);
                    }
                }

                PlaceInNewSlot(slot);
            }
            else
            {
                ReturnToOriginalSlot();
            }
        }
    }

    private void HandleUIDrop(DropZone targetZone)
    {
        if (targetZone == null || targetZone.zoneType != DropZone.ZoneType.GameSlot)
        {
            return;
        }

        ObjectSlot slot = targetZone.associatedSlot;
        if (slot == null || slot.IsOccupied())
        {
            return;
        }

        if (currentUIDraggable.itemData == null || currentUIDraggable.itemData.itemPrefab == null)
        {
            Debug.LogError($"UIアイテム '{currentUIDraggable.name}' のItemDataまたはPrefabが正しく設定されていません。", currentUIDraggable);
            return;
        }

        // 正解・不正解を判定してSEを再生
        // このスロットの正解タイプと、置いたアイテムのタイプが一致するか？
        if (slot.correctItemType == currentUIDraggable.itemData.itemType)
        {
            // 正解のSEを再生
            if (audioSource != null && correctPlacementSound != null)
            {
                audioSource.PlayOneShot(correctPlacementSound, correctPlacementVolume);
            }
        }
        else
        {
            // 不正解のSEを再生
            if (audioSource != null && incorrectPlacementSound != null)
            {
                audioSource.PlayOneShot(incorrectPlacementSound, incorrectPlacementVolume);
            }
        }

        // 全てのチェックを通過した場合のみ、アイテムを生成して配置
        GameObject newItem = Instantiate(
            currentUIDraggable.itemData.itemPrefab,
            slot.slotTransform.position,
            Quaternion.identity
        );

        Draggable newDraggable = newItem.GetComponent<Draggable>();
        if (newDraggable != null)
        {
            newDraggable.itemData = currentUIDraggable.itemData;
            slot.currentObject = newDraggable;
            newDraggable.currentSlot = slot;
            currentUIDraggable.MarkAsUsed();
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