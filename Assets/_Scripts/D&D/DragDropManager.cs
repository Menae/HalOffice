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
    public Camera mainCamera;
    public Camera uiCamera;

    private Draggable currentDraggedObject;
    private UIDraggable currentUIDraggable;

    private ObjectSlot originalSlot;
    private Vector3 originalPosition;
    private Vector2 dragOffset;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }

        if (mainCamera == null) { mainCamera = Camera.main; }
    }

    private void Update()
    {
        if (dragProxyImage.gameObject.activeSelf)
        {
            // 現在のマウス位置をCanvasのローカル座標に変換
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                Input.mousePosition,
                parentCanvas.worldCamera,
                out localPoint);

            // UIプロキシの位置を、マウス位置からオフセット分だけずらして調整する
            dragProxyImage.rectTransform.localPosition = localPoint - dragOffset;

            if (Input.GetMouseButtonUp(0))
            {
                HandleDrop();
            }
        }
    }

    public void StartDragFromInputBridge(PointerEventData eventData, LayerMask layerMask)
    {
        if (screenToWorldConverter.GetWorldPosition(eventData.position, out Vector3 worldPos))
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos, layerMask);
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
        if (!IsDraggable()) return;

        currentDraggedObject = draggable;
        originalPosition = draggable.transform.position;
        originalSlot = draggable.currentSlot; // オブジェクトが覚えているスロットを直接取得
        if (originalSlot != null)
        {
            originalSlot.currentObject = null;
            draggable.currentSlot = null;      // ドラッグ中はどのスロットにも属さない
        }

        SpriteRenderer sr = draggable.GetComponent<SpriteRenderer>();
        Vector2 objectScreenPos = mainCamera.WorldToScreenPoint(draggable.transform.position);
        SetupProxy(sr.sprite);

        // 1. スプライトのローカル空間での境界を取得
        Bounds localBounds = sr.sprite.bounds;

        // 2. 境界の四隅の点をワールド座標に変換
        Vector3[] worldCorners = new Vector3[4];
        worldCorners[0] = draggable.transform.TransformPoint(new Vector3(localBounds.min.x, localBounds.min.y, 0)); // 左下
        worldCorners[1] = draggable.transform.TransformPoint(new Vector3(localBounds.min.x, localBounds.max.y, 0)); // 左上
        worldCorners[2] = draggable.transform.TransformPoint(new Vector3(localBounds.max.x, localBounds.max.y, 0)); // 右上
        worldCorners[3] = draggable.transform.TransformPoint(new Vector3(localBounds.max.x, localBounds.min.y, 0)); // 右下

        // 3. 各ワールド座標をRawImage上のローカルUI座標に変換し、その最大と最小を探す
        Rect rawImageRect = screenToWorldConverter.gameScreen.rectTransform.rect;
        Vector2 localMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 localMax = new Vector2(float.MinValue, float.MinValue);

        foreach (Vector3 worldPos in worldCorners)
        {
            // ワールド座標をViewport座標(0-1の割合)に変換
            Vector2 viewportPoint = mainCamera.WorldToViewportPoint(worldPos);

            // Viewport座標を、ゲーム画面を描画しているRawImageのローカル座標に変換
            Vector2 localPoint = new Vector2(
                Mathf.Lerp(rawImageRect.xMin, rawImageRect.xMax, viewportPoint.x),
                Mathf.Lerp(rawImageRect.yMin, rawImageRect.yMax, viewportPoint.y)
            );

            // ローカル座標の最小値と最大値を更新していく
            localMin = Vector2.Min(localMin, localPoint);
            localMax = Vector2.Max(localMax, localPoint);
        }

        // 4. ローカル座標の最大と最小の差から、代理UIのサイズ(sizeDelta)を直接決定する
        dragProxyImage.rectTransform.sizeDelta = localMax - localMin;

        // 5. マウスのクリック座標と、オブジェクトの見た目の中心との差分を計算して記憶する
        //    これにより、ピボTットがずれていても掴んだ位置がずれないようにする
        Vector2 proxyCenterLocalPos = (localMin + localMax) / 2f;
        Vector2 mouseLocalPos;
        // 計算の基準を「parentCanvas」から「gameScreenのRectTransform」に変更し、
        // 使用するカメラもUIカメラに統一する
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            screenToWorldConverter.gameScreen.rectTransform, // CanvasではなくRawImageを基準にする
            screenPos,
            uiCamera,                                        // 使用するカメラをUIカメラに指定
            out mouseLocalPos
        );
        dragOffset = mouseLocalPos - proxyCenterLocalPos;

        currentDraggedObject.gameObject.SetActive(false);
    }

    public void StartDragFromUI(UIDraggable uiDraggable, PointerEventData eventData)
    {
        if (!IsDraggable()) return;

        currentUIDraggable = uiDraggable;
        Image sourceImage = uiDraggable.GetComponent<Image>();

        // 1. 代理UIの見た目を設定（単純化されたメソッドを呼び出す）
        SetupProxy(sourceImage.sprite);
        dragProxyImage.rectTransform.sizeDelta = sourceImage.rectTransform.sizeDelta;

        // 2. マウスのクリック位置をCanvasのローカル座標に変換
        Vector2 mouseLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            parentCanvas.worldCamera,
            out mouseLocalPos
        );

        // 3. ドラッグ対象UIのローカル座標を取得し、オフセットを計算
        //    (sourceImage.rectTransform.localPositionは親（Canvas）からの相対位置)
        dragOffset = mouseLocalPos - (Vector2)sourceImage.rectTransform.localPosition;
    }

    private void SetupProxy(Sprite sprite)
    {
        dragProxyImage.sprite = sprite;
        dragProxyImage.gameObject.SetActive(true);

        // dragOffsetの計算は各StartDrag...メソッドに移動するため、ここでは削除
    }

    // HandleDrop, FindDropZoneUnderCursorなどのメソッドは変更なし
    private void HandleDrop()
    {
        dragProxyImage.gameObject.SetActive(false);
        if (currentDraggedObject == null && currentUIDraggable == null) return;

        DropZone targetZone = FindDropZoneUnderCursor();
        if (currentDraggedObject != null) HandleGameWorldDrop(targetZone);
        else if (currentUIDraggable != null) HandleUIDrop(targetZone);

        currentDraggedObject = null;
        currentUIDraggable = null;
        originalSlot = null;
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
            if (targetZone != null && targetZone.zoneType == DropZone.ZoneType.GameSlot && !targetZone.associatedSlot.IsOccupied())
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
        // ドロップが有効かチェックする（変更なし）
        if (targetZone != null && targetZone.zoneType == DropZone.ZoneType.GameSlot && !targetZone.associatedSlot.IsOccupied())
        {
            // アイテムを生成する（変更なし）
            GameObject newItem = Instantiate(
                currentUIDraggable.itemPrefab,
                targetZone.associatedSlot.slotTransform.position,
                Quaternion.identity
            );

            // ▼▼▼ ここからが修正部分です ▼▼▼

            // 1. Draggableコンポーネントを安全に取得する
            Draggable newDraggable = newItem.GetComponent<Draggable>();

            // 2. コンポーネントが存在するかチェックする「ガード節」を追加
            if (newDraggable == null)
            {
                // もしプレハブにDraggableスクリプトがなければ、エラーを記録して処理を停止する
                Debug.LogError($"プレハブ '{currentUIDraggable.itemPrefab.name}' に Draggable コンポーネントがアタッチされていません！", currentUIDraggable.gameObject);
                Destroy(newItem); // 設定が間違っているオブジェクトを掃除する
                return;
            }

            // 3. もしチェックを通過したら、元のロジックを続行する
            targetZone.associatedSlot.currentObject = newDraggable;
            newDraggable.currentSlot = targetZone.associatedSlot; // この行はもうエラーを引き起こしません
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
            currentDraggedObject.transform.position = originalPosition;
            if (originalSlot != null)
            {
                originalSlot.currentObject = currentDraggedObject;

                currentDraggedObject.currentSlot = originalSlot;
            }
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