using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI上のアイテムをドラッグ＆ドロップで操作するコンポーネント。
/// アイテムのクリック、ドラッグ開始、ドラッグ中、ドラッグ終了を管理し、
/// DragDropManagerと連携して実際のドラッグ処理を委譲する。
/// </summary>
public class UIDraggable : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>
    /// 該当UIが表すアイテムのデータ。Inspectorで割り当てるScriptableObject。
    /// </summary>
    public ItemData itemData;

    /// <summary>
    /// ドラッグ中の見た目の拡大率。Inspectorで調整可能。
    /// デフォルトは1.5（1.0が等倍）。
    /// </summary>
    public float dragScale = 1.5f;

    /// <summary>
    /// ドラッグ中に配置可能な場所で表示するハイライト用GameObject（任意）。
    /// ハイライトの有効/無効はSetHighlightで制御。
    /// </summary>
    public GameObject highlightGraphic;

    // 使用済みフラグ。使用済みのアイテムはクリックやドラッグを受け付けない。
    private bool isUsed = false;

    // 親のScrollRect参照。ドラッグ時にスクロールと競合するため一時無効化する。
    private ScrollRect parentScrollRect;

    // CanvasGroup参照。ドラッグ中にblocksRaycastsを切り替えるために使用。
    private CanvasGroup canvasGroup;

    /// <summary>
    /// UnityのStartイベント。インスタンス生成後、最初のフレーム前に1回呼ばれる。
    /// 初期状態としてハイライトを無効化する。
    /// </summary>
    private void Start()
    {
        SetHighlight(false);
    }

    /// <summary>
    /// UI要素のクリックイベント。左クリックでDragDropManagerに通知。
    /// isUsedがtrueの場合は無視。
    /// </summary>
    /// <param name="eventData">クリックに関するイベント情報。</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isUsed) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        DragDropManager.Instance.HandleItemClick(this, null, eventData);
    }

    /// <summary>
    /// ドラッグ開始処理。左クリックで開始し、親ScrollRectを一時無効化して
    /// CanvasGroupのレイキャストブロックを解除してからDragDropManagerへ通知する。
    /// </summary>
    /// <param name="eventData">ドラッグ開始に関するイベント情報。</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUsed) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 親の ScrollRect を一時的に無効化。ドラッグとスクロールの競合回避。
        if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
        if (parentScrollRect != null) parentScrollRect.enabled = false;

        // ドロップ先のUIやワールドへレイキャストを通すためにblocksRaycastsを解除。
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        DragDropManager.Instance.HandleBeginDragUI(this, eventData);
    }

    /// <summary>
    /// ドラッグ中の更新処理。DragDropManagerへイベントを渡して処理を委譲する。
    /// isUsedがtrueの場合は無視。
    /// </summary>
    /// <param name="eventData">ドラッグ中のイベント情報。</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (isUsed) return;
        DragDropManager.Instance.HandleDrag(eventData);
    }

    /// <summary>
    /// ドラッグ終了処理。DragDropManagerへ通知し、無効化した親ScrollRectや
    /// CanvasGroupの状態を元に戻す。
    /// </summary>
    /// <param name="eventData">ドラッグ終了に関するイベント情報。</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isUsed) return;

        DragDropManager.Instance.HandleEndDrag(eventData);

        // 状態を復帰。
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        if (parentScrollRect != null) parentScrollRect.enabled = true;
    }

    /// <summary>
    /// アイテムを使用済みに設定し、見た目とボタンの相互作用を無効化する。
    /// 既に使用済みのアイテムはクリックやドラッグを受け付けない。
    /// </summary>
    public void MarkAsUsed()
    {
        isUsed = true;
        GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        if (GetComponent<Button>() != null)
        {
            GetComponent<Button>().interactable = false;
        }
    }

    /// <summary>
    /// 使用状態と見た目をリセットし、ボタンがあれば再度操作可能にする。
    /// </summary>
    public void ResetState()
    {
        isUsed = false;
        GetComponent<Image>().color = Color.white;
        if (GetComponent<Button>() != null) GetComponent<Button>().interactable = true;
    }

    /// <summary>
    /// ハイライト表示の切替。nullチェックを行い、安全に有効/無効を設定する。
    /// </summary>
    /// <param name="isActive">ハイライトを有効にする場合はtrue、無効化する場合はfalse。</param>
    public void SetHighlight(bool isActive)
    {
        if (highlightGraphic != null)
        {
            highlightGraphic.SetActive(isActive);
        }
    }
}