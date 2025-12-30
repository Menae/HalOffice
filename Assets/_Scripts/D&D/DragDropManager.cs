using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropManager : MonoBehaviour
{
    /// <summary>
    /// ドラッグ操作が開始されたときに発行するグローバルイベント。
    /// UIやゲームロジックがドラッグ開始を検知するために購読する。
    /// </summary>
    public static event Action OnDragStarted;

    /// <summary>
    /// ドラッグ操作が終了したときに発行するグローバルイベント。
    /// UIやゲームロジックがドラッグ終了を検知するために購読する。
    /// </summary>
    public static event Action OnDragEnded;

    /// <summary>
    /// アイテムがスロットに正常に配置されたときに発行するイベント。
    /// シーン内の配置達成処理を通知するために使用する。
    /// </summary>
    public event Action OnItemPlaced;

    /// <summary>
    /// アイテムがゴミ箱へ捨てられたときに発行するイベント。
    /// ゴミ箱処理やスコア更新などを通知するために使用する。
    /// </summary>
    public event Action OnItemTrashed;

    /// <summary>
    /// 現在アクティブな CursorController の参照。
    /// CursorController は自身を登録/解除してこのプロパティに反映する。
    /// </summary>
    public static CursorController ActiveCursor { get; private set; }

    /// <summary>
    /// CursorController が自身を登録するためのメソッド。
    /// 登録されたカーソルはドラッグ中のプロキシの親になる。
    /// </summary>
    /// <param name="cursor">登録する CursorController。</param>
    public static void RegisterCursor(CursorController cursor)
    {
        ActiveCursor = cursor;
    }

    /// <summary>
    /// CursorController が自身の登録を解除するためのメソッド。
    /// 現在登録されているインスタンスと一致する場合のみ解除する。
    /// </summary>
    /// <param name="cursor">解除を要求する CursorController。</param>
    public static void UnregisterCursor(CursorController cursor)
    {
        if (ActiveCursor == cursor)
        {
            ActiveCursor = null;
        }
    }

    private enum DdState { Idle, ItemSelected, HoldingItem }
    private DdState currentState = DdState.Idle;

    [Header("参照")]
    [Tooltip("ドラッグ対象として判定する物理レイヤー")]
    /// <summary>
    /// ドラッグ可能と見なす物理レイヤー設定。Inspectorで設定必須。
    /// </summary>
    public LayerMask draggableLayer;

    /// <summary>
    /// スクリーン座標をゲーム世界座標へ変換するユーティリティ参照。
    /// RenderTexture を利用する場合は正しく設定すること。
    /// </summary>
    public ScreenToWorldConverter screenToWorldConverter;

    /// <summary>
    /// 現在利用中の EventSystem。Raycast に使用する。
    /// </summary>
    public EventSystem eventSystem;

    [Tooltip("ドラッグ操作のルートオブジェクト（移動させる親）")]
    /// <summary>
    /// ドラッグ中に表示するプロキシのルート RectTransform。Inspectorで設定必須。
    /// </summary>
    public RectTransform dragProxyRoot;

    [Tooltip("アイテム本体の画像を表示するImage")]
    /// <summary>
    /// ドラッグプロキシでアイテム本体を表示する Image。raycastTarget は無効化する。
    /// </summary>
    public Image dragProxyItemImage;

    [Tooltip("ハイライト（後光）を表示するImage")]
    /// <summary>
    /// ドラッグプロキシでハイライトを表示する Image。サイズやオフセットは ItemData 側で指定する。
    /// </summary>
    public Image dragProxyHighlightImage;

    /// <summary>
    /// ルート Canvas。プロキシを一時的に戻す際に使用する。
    /// </summary>
    public Canvas parentCanvas;

    /// <summary>
    /// ゲーム世界を描画するメインカメラ。ワールド座標⇔スクリーン座標変換に使用。
    /// </summary>
    public Camera mainCamera;

    /// <summary>
    /// UI レンダリング用カメラ（必要な場合に使用）。
    /// </summary>
    public Camera uiCamera;

    [Header("サウンド設定")]
    [Tooltip("効果音を再生するためのAudioSource")]
    /// <summary>
    /// 効果音再生に使う AudioSource。null の場合は再生をスキップする。
    /// </summary>
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

    // 内部変数
    private Draggable currentDraggedObject;
    private Draggable selectedObject;
    private UIDraggable currentUIDraggable;
    private UIDraggable selectedUIDraggable;
    private ObjectSlot originalSlot;

    /// <summary>
    /// シーン内のシングルトン参照。複数存在する場合は後から生成されたものを破棄する。
    /// </summary>
    public static DragDropManager Instance { get; private set; }

    /// <summary>
    /// ユーザーからの操作を受け付けるかどうかのフラグ。false の場合は全ての入力を無視する。
    /// Inspector から直接変更しないこと。SetInteractionEnabled 経由で制御すること。
    /// </summary>
    public bool InteractionEnabled { get; private set; } = true;

    /// <summary>
    /// 入力受付状態を切り替える。カーソルにも同じ命令を伝播する。
    /// </summary>
    /// <param name="enabled">入力を許可するなら true。</param>
    public void SetInteractionEnabled(bool enabled)
    {
        InteractionEnabled = enabled;

        if (ActiveCursor != null)
        {
            ActiveCursor.SetInputEnabled(enabled);
        }
    }

    /// <summary>
    /// Awake は Unity の初期化フェーズで呼ばれる。シングルトンの登録を行う。
    /// 他のオブジェクトの Awake より後に実行される依存がある場合は注意。
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; }
    }

    /// <summary>
    /// アイテムがクリックされたときの処理。
    /// UI 由来かワールド由来かを判断し、選択状態の切替とダイアログ表示を行う。
    /// null チェックを行い、同一アイテムの再クリックで選択解除する。
    /// </summary>
    /// <param name="uiDraggable">UI 由来のドラッグ可能オブジェクト（存在しない場合は null）。</param>
    /// <param name="worldDraggable">ワールド由来のドラッグ可能オブジェクト（存在しない場合は null）。</param>
    /// <param name="eventData">クリックに関する PointerEventData。</param>
    public void HandleItemClick(UIDraggable uiDraggable, Draggable worldDraggable, PointerEventData eventData)
    {
        if (!InteractionEnabled) return;
        if (currentState == DdState.HoldingItem) return;

        object clickedItem = uiDraggable != null ? (object)uiDraggable : (object)worldDraggable;
        object previouslySelectedItem = selectedUIDraggable != null ? (object)selectedUIDraggable : (object)selectedObject;

        // 以前に選択されていたものがあれば、ハイライトを消す
        if (previouslySelectedItem != null)
        {
            if (selectedUIDraggable != null) selectedUIDraggable.SetHighlight(false);
            if (selectedObject != null) selectedObject.SetHighlight(false);
        }

        // 何もクリックされなかったか、同じアイテムを再度クリックした場合は選択解除して Idle に戻す
        if (clickedItem == null || clickedItem == previouslySelectedItem)
        {
            selectedUIDraggable = null;
            selectedObject = null;
            currentState = DdState.Idle;
        }
        else
        {
            if (uiDraggable != null)
            {
                selectedUIDraggable = uiDraggable;
                selectedObject = null;
                selectedUIDraggable.SetHighlight(true);

                if (selectedUIDraggable.itemData?.descriptionInk != null)
                    InkDialogueManager.Instance.ShowStory(selectedUIDraggable.itemData.descriptionInk);
            }
            else
            {
                selectedObject = worldDraggable;
                selectedUIDraggable = null;
                selectedObject.SetHighlight(true);

                if (selectedObject.itemData?.descriptionInk != null)
                    InkDialogueManager.Instance.ShowStory(selectedObject.itemData.descriptionInk);
            }
            currentState = DdState.ItemSelected;
        }
    }

    /// <summary>
    /// ワールド側の Draggable からドラッグが開始されたときの処理。
    /// 選択済みから持ち上げるケースと、Idle から直接ドラッグするケースの両方を扱う。
    /// </summary>
    /// <param name="draggedObject">開始された Draggable。</param>
    /// <param name="eventData">ドラッグ開始に関する PointerEventData（位置参照に使用）。</param>
    public void HandleBeginDrag(Draggable draggedObject, PointerEventData eventData)
    {
        if (!InteractionEnabled) return;

        if (currentState == DdState.ItemSelected && draggedObject == selectedObject)
        {
            StartHolding(draggedObject, eventData.position);
        }
        else if (currentState == DdState.Idle && draggedObject != null)
        {
            selectedObject = draggedObject;
            selectedObject.SetHighlight(true);
            currentState = DdState.ItemSelected;
            StartHolding(draggedObject, eventData.position);
        }
    }

    /// <summary>
    /// UI 側の UIDraggable からドラッグが開始されたときの処理。
    /// デバッグログを残しつつ、選択状態の遷移とプロキシ表示の初期化を行う。
    /// </summary>
    /// <param name="draggedObject">開始された UIDraggable。</param>
    /// <param name="eventData">ドラッグ開始に関する PointerEventData。</param>
    public void HandleBeginDragUI(UIDraggable draggedObject, PointerEventData eventData)
    {
        Debug.Log($"--- DragDropManager.HandleBeginDragUI ---: 呼び出されました。");
        Debug.Log($"現在の状態(currentState): {currentState}");
        Debug.Log($"選択中のUIオブジェクト(selectedUIDraggable): {(selectedUIDraggable != null ? selectedUIDraggable.name : "null")}");
        Debug.Log($"ドラッグされたオブジェクト(draggedObject): {(draggedObject != null ? draggedObject.name : "null")}");

        if (!InteractionEnabled) return;

        if (currentState == DdState.ItemSelected && draggedObject == selectedUIDraggable)
        {
            Debug.Log("条件成功：ドラッグを開始します。");
            StartHoldingUI(draggedObject, eventData);
        }
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
            Debug.LogWarning("条件失敗：ドラッグを開始できませんでした。");
        }
    }

    /// <summary>
    /// ドラッグ中の毎フレーム処理。プロキシのハイライト表示判定と反映を行う。
    /// 追加: スロット側のハイライト制御
    /// </summary>
    public void HandleDrag(PointerEventData eventData)
    {
        if (currentState != DdState.HoldingItem) return;

        ItemData currentItemData = null;
        if (currentDraggedObject != null) currentItemData = currentDraggedObject.itemData;
        else if (currentUIDraggable != null) currentItemData = currentUIDraggable.itemData;

        if (currentItemData == null) return;

        bool showProxyHighlight = false;

        // カーソル下のドロップゾーンを取得
        DropZone zoneUnderCursor = FindDropZoneUnderCursor();

        // 全てのDropZoneのハイライトを一旦リセット（重ければ最適化可）
        // ※シーン内の全DropZoneを取得するのは重いので、本来はキャッシュすべきだが、
        // 簡易実装として直前にハイライトしたものを記憶する変数をクラスに追加するのがベスト。
        // ここでは簡略化のため、「現在ホバー中のものだけON、それ以外OFF」というロジックにする。

        // スロットハイライト処理
        if (zoneUnderCursor != null && zoneUnderCursor.zoneType == DropZone.ZoneType.GameSlot)
        {
            ObjectSlot slot = zoneUnderCursor.associatedSlot;
            // スロットが空き、かつ受け入れ可能であれば
            if (slot != null && !slot.IsOccupied() && slot.CanAccept(currentItemData.itemType))
            {
                showProxyHighlight = true; // 手持ちアイテムのハイライト

                // スロットが正解であればスロット自体をハイライト
                if (slot.IsCorrectItem(currentItemData.itemType))
                {
                    zoneUnderCursor.SetHighlight(true);
                }
            }
        }

        // 前フレームでハイライトしていたものを消す処理が必要。
        // HandleDrag内で簡易的に行うなら、FindDropZoneUnderCursorで取得できなかった
        // 他のすべてのDropZoneのSetHighlight(false)を呼ぶ必要があるが、
        // 実装の手間を減らすため、ResetAllSlotHighlightsヘルパーメソッドを追加し、ここで呼ぶ。
        ResetAllSlotHighlights(zoneUnderCursor);

        if (dragProxyHighlightImage != null)
        {
            dragProxyHighlightImage.gameObject.SetActive(showProxyHighlight);
        }
    }

    // ハイライトリセット用のヘルパー
    private DropZone lastHighlightedZone;

    private void ResetAllSlotHighlights(DropZone currentZone)
    {
        // 以前ハイライトしていたが、現在対象でないなら消す
        if (lastHighlightedZone != null && lastHighlightedZone != currentZone)
        {
            lastHighlightedZone.SetHighlight(false);
            lastHighlightedZone = null;
        }

        // 現在ハイライトすべきゾーンがあれば記録
        if (currentZone != null)
        {
            lastHighlightedZone = currentZone;
        }
    }

    /// <summary>
    /// ドラッグが終了したときの処理。
    /// HoldingItem 状態であればドロップ処理へ委譲する。
    /// </summary>
    /// <param name="eventData">終了時の PointerEventData。</param>
    public void HandleEndDrag(PointerEventData eventData)
    {
        if (currentState == DdState.HoldingItem)
        {
            HandleDrop(eventData);
        }
    }

    /// <summary>
    /// 世界側の Draggable を持ち上げる開始処理。
    /// プロキシの初期化、元スロットからの切り離し、表示の切替を行う。
    /// </summary>
    /// <param name="draggable">持ち上げる Draggable。</param>
    /// <param name="screenPos">マウスのスクリーン座標（初期オフセット計算に使用）。</param>
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

        ItemData data = draggable.itemData;
        Sprite highlightSprite = (data != null) ? data.highlightSprite : null;

        Vector2 offset = (data != null) ? data.highlightOffset : Vector2.zero;

        Vector2 scale = (data != null) ? data.highlightScale : Vector2.one;

        SetupProxy(sr.sprite, highlightSprite, offset, scale);

        Bounds worldBounds = sr.bounds;
        Vector2 minScreenPoint = mainCamera.WorldToScreenPoint(worldBounds.min);
        Vector2 maxScreenPoint = mainCamera.WorldToScreenPoint(worldBounds.max);
        Vector2 pixelSize = maxScreenPoint - minScreenPoint;

        dragProxyRoot.sizeDelta = new Vector2(Mathf.Abs(pixelSize.x), Mathf.Abs(pixelSize.y));
        dragProxyRoot.localScale = Vector3.one;

        if (ActiveCursor != null)
        {
            dragProxyRoot.SetParent(ActiveCursor.cursorImage.transform, true);
            dragProxyRoot.anchoredPosition = Vector2.zero;
        }

        currentDraggedObject.gameObject.SetActive(false);
        OnDragStarted?.Invoke();
    }

    /// <summary>
    /// UI 側の UIDraggable を持ち上げる開始処理。
    /// UI 用の画像、スケール、オフセットをプロキシへ適用する。
    /// </summary>
    /// <param name="uiDraggable">持ち上げる UIDraggable。</param>
    /// <param name="eventData">ドラッグ開始に関する PointerEventData。</param>
    private void StartHoldingUI(UIDraggable uiDraggable, PointerEventData eventData)
    {
        currentState = DdState.HoldingItem;
        currentUIDraggable = uiDraggable;

        Image sourceImage = uiDraggable.GetComponent<Image>();

        ItemData data = uiDraggable.itemData;
        Sprite highlightSprite = (data != null) ? data.highlightSprite : null;

        Vector2 offset = (data != null) ? data.highlightOffset : Vector2.zero;

        Vector2 scale = (data != null) ? data.highlightScale : Vector2.one;

        SetupProxy(sourceImage.sprite, highlightSprite, offset, scale);

        dragProxyRoot.sizeDelta = sourceImage.rectTransform.sizeDelta;
        dragProxyRoot.localScale = Vector3.one * uiDraggable.dragScale;

        if (ActiveCursor != null)
        {
            dragProxyRoot.SetParent(ActiveCursor.cursorImage.transform, true);
            dragProxyRoot.anchoredPosition = Vector2.zero;
        }

        OnDragStarted?.Invoke();
    }

    /// <summary>
    /// ドラッグ中に表示するプロキシを初期化する。
    /// ルートの CanvasGroup 設定、画像の差し替え、位置・スケールの適用を行う。
    /// </summary>
    /// <param name="mainSprite">アイテム本体のスプライト。</param>
    /// <param name="highlightSprite">ハイライト用スプライト（ない場合は null）。</param>
    /// <param name="offset">ハイライトのオフセット。</param>
    /// <param name="scale">ハイライトのスケール（X, Y）。</param>
    private void SetupProxy(Sprite mainSprite, Sprite highlightSprite, Vector2 offset, Vector2 scale)
    {
        dragProxyRoot.gameObject.SetActive(true);

        CanvasGroup rootGroup = dragProxyRoot.GetComponent<CanvasGroup>();
        if (rootGroup == null) rootGroup = dragProxyRoot.gameObject.AddComponent<CanvasGroup>();
        rootGroup.blocksRaycasts = false;
        rootGroup.interactable = false;

        if (dragProxyItemImage != null)
        {
            dragProxyItemImage.sprite = mainSprite;
            dragProxyItemImage.raycastTarget = false;
            dragProxyItemImage.gameObject.SetActive(true);
            dragProxyItemImage.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (dragProxyHighlightImage != null)
        {
            dragProxyHighlightImage.sprite = highlightSprite;
            dragProxyHighlightImage.raycastTarget = false;

            dragProxyHighlightImage.rectTransform.anchoredPosition = offset;
            dragProxyHighlightImage.rectTransform.localScale = new Vector3(scale.x, scale.y, 1f);

            dragProxyHighlightImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ドロップ処理のエントリポイント。プロキシの後処理、ドロップ先判定、各種イベント発火を行う。
    /// </summary>
    /// <param name="eventData">ドロップ時の PointerEventData。</param>
    private void HandleDrop(PointerEventData eventData)
    {
        OnDragEnded?.Invoke();

        // プロキシをキャンバスへ戻して非表示にする
        dragProxyRoot.SetParent(parentCanvas.transform, true);
        dragProxyRoot.gameObject.SetActive(false);

        if (InkDialogueManager.Instance != null) { InkDialogueManager.Instance.CloseDialogue(); }

        DropZone targetZone = FindDropZoneUnderCursor();

        if (currentDraggedObject != null)
        {
            HandleGameWorldDrop(targetZone);
            currentDraggedObject.SetHighlight(false);
        }
        else if (currentUIDraggable != null)
        {
            HandleUIDrop(targetZone);
            currentUIDraggable.SetHighlight(false);
        }

        // 状態のリセット
        currentState = DdState.Idle;
        selectedObject = null;
        selectedUIDraggable = null;
        currentDraggedObject = null;
        currentUIDraggable = null;
        originalSlot = null;
    }

    /// <summary>
    /// ゲーム空間内のオブジェクトがドロップされた際の処理を行う。
    /// ゴミ箱への廃棄、または新しいスロットへの配置可否を判定し、実行する。
    /// </summary>
    /// <param name="targetZone">ドロップ先のゾーン情報</param>
    private void HandleGameWorldDrop(DropZone targetZone)
    {
        // ドロップ操作完了に伴い、直前まで表示していたハイライトを解除する
        if (lastHighlightedZone != null)
        {
            lastHighlightedZone.SetHighlight(false);
            lastHighlightedZone = null;
        }

        // ゴミ箱へのドロップ判定
        if (targetZone != null && targetZone.zoneType == DropZone.ZoneType.TrashCan)
        {
            if (audioSource != null && trashSound != null)
            {
                audioSource.PlayOneShot(trashSound, trashVolume);
            }

            ObjectSlot removedFromSlot = originalSlot;

            // オブジェクトを破棄する
            Destroy(currentDraggedObject.gameObject);

            // 破棄イベントを発火する
            GameEventManager.InvokeObjectRemoved(removedFromSlot);
            OnItemTrashed?.Invoke();
        }
        else
        {
            // ドロップ失敗またはスロット移動の判定へ進む
            // ドラッグ中は非表示にしていたオブジェクトを表示に戻す
            currentDraggedObject.gameObject.SetActive(true);

            // 有効なゲームスロットへのドロップか判定を行う
            if (targetZone != null &&
                targetZone.zoneType == DropZone.ZoneType.GameSlot &&
                !targetZone.associatedSlot.IsOccupied() &&
                targetZone.associatedSlot.CanAccept(currentDraggedObject.itemData.itemType))
            {
                ObjectSlot slot = targetZone.associatedSlot;

                // 正誤判定：リストを用いた判定メソッドを使用し、正解・不正解のSEを再生する
                if (slot.IsCorrectItem(currentDraggedObject.itemData.itemType))
                {
                    if (audioSource != null && correctPlacementSound != null)
                    {
                        audioSource.PlayOneShot(correctPlacementSound, correctPlacementVolume);
                    }
                }
                else
                {
                    if (audioSource != null && incorrectPlacementSound != null)
                    {
                        audioSource.PlayOneShot(incorrectPlacementSound, incorrectPlacementVolume);
                    }
                }

                // 新しいスロットへオブジェクトを配置する
                PlaceInNewSlot(slot);
                OnItemPlaced?.Invoke();
            }
            else
            {
                // 配置不可能な場所へのドロップ時は、元のスロット位置に戻す
                ReturnToOriginalSlot();
            }
        }
    }

    /// <summary>
    /// UI上のアイテム（タスクバー等）がドロップされた際の処理を行う。
    /// 実体化してワールド内のスロットへ配置するフローを制御する。
    /// </summary>
    /// <param name="targetZone">ドロップ先のゾーン情報</param>
    private void HandleUIDrop(DropZone targetZone)
    {
        if (targetZone == null) return;

        // ドロップ操作完了に伴い、ハイライトを解除する
        if (lastHighlightedZone != null)
        {
            lastHighlightedZone.SetHighlight(false);
            lastHighlightedZone = null;
        }

        // ゴミ箱へのドロップ判定
        if (targetZone.zoneType == DropZone.ZoneType.TrashCan)
        {
            if (audioSource != null && trashSound != null)
            {
                audioSource.PlayOneShot(trashSound, trashVolume);
            }

            // UIアイテムを使用済みにする
            currentUIDraggable.MarkAsUsed();

            // ゴミ捨てイベントを発行（チュートリアル進行用）
            OnItemTrashed?.Invoke();
            return;
        }

        // チュートリアルゾーンへのドロップ判定
        // 実際のアイテム生成は行わないが、配置成功イベントは発行する必要がある
        if (targetZone.isTutorialZone)
        {
            // 配置イベントを発行（チュートリアル進行用）
            OnItemPlaced?.Invoke();

            // 視覚的なフィードバックとして使用済みにする
            currentUIDraggable.MarkAsUsed();
            return;
        }

        // ゲームスロット以外へのUIからの直接ドロップは許可しない
        if (targetZone.zoneType != DropZone.ZoneType.GameSlot) return;

        ObjectSlot slot = targetZone.associatedSlot;

        // スロットが無効、既に埋まっている、あるいはアイテムタイプを受け入れない場合は中断する
        if (slot == null || slot.IsOccupied() || !slot.CanAccept(currentUIDraggable.itemData.itemType))
        {
            return;
        }

        // 配置処理を開始する
        // 正誤判定：リストベースの判定メソッドを利用する
        if (slot.IsCorrectItem(currentUIDraggable.itemData.itemType))
        {
            if (audioSource != null && correctPlacementSound != null)
            {
                audioSource.PlayOneShot(correctPlacementSound, correctPlacementVolume);
            }
        }
        else
        {
            if (audioSource != null && incorrectPlacementSound != null)
            {
                audioSource.PlayOneShot(incorrectPlacementSound, incorrectPlacementVolume);
            }
        }

        // UIアイテムに対応するプレハブをワールド空間に生成する
        GameObject newItem = Instantiate(currentUIDraggable.itemData.itemPrefab, slot.slotTransform.position, Quaternion.identity);

        Draggable newDraggable = newItem.GetComponent<Draggable>();
        if (newDraggable != null)
        {
            // データを引き継ぎ、スロットとの相互参照を設定する
            newDraggable.itemData = currentUIDraggable.itemData;
            slot.currentObject = newDraggable;
            newDraggable.currentSlot = slot;

            // UI側のアイテムを使用済みにする
            currentUIDraggable.MarkAsUsed();

            // 新規配置フラグを立てる（評価ロジック等で使用）
            if (FindObjectOfType<ObjectSlotManager>() != null)
            {
                FindObjectOfType<ObjectSlotManager>().MarkSlotAsNewlyPlaced(slot);
            }

            OnItemPlaced?.Invoke();
        }
    }

    /// <summary>
    /// カーソル下にある DropZone を検索して返す。
    /// UI の Raycast 結果を優先し、見つからなければワールドの Collider2D を調べる。
    /// </summary>
    /// <returns>見つかった DropZone、存在しない場合は null。</returns>
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

    /// <summary>
    /// ドラッグ中のオブジェクトを元のスロットに戻す処理。
    /// originalSlot が null の場合は何もしない。
    /// </summary>
    private void ReturnToOriginalSlot()
    {
        if (currentDraggedObject != null)
        {
            if (originalSlot != null)
            {
                currentDraggedObject.transform.position = originalSlot.slotTransform.position;
                originalSlot.currentObject = currentDraggedObject;
                currentDraggedObject.currentSlot = originalSlot;
            }
        }
    }

    /// <summary>
    /// 指定したスロットに現在のドラッグ対象を配置する。
    /// transform の位置更新とスロット参照の更新を行う。
    /// </summary>
    /// <param name="newSlot">配置先の ObjectSlot。</param>
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