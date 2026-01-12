using UnityEngine;

/// <summary>
/// ゲーム内のスロット状況（アイテムの配置状態）を常時監視し、
/// 知識量・自律性・完成度の各メーター（RetroMeter）へ値を反映させるマネージャークラス。
/// </summary>
/// <remarks>
/// ユーザー体験を損なわないよう、アイテムのドラッグ＆ドロップ操作中のメーター変動を抑制する機能を実装。
/// カーソルが指定されたゲーム画面範囲内にある場合は、移動中のアイテムを
/// 仮想的に「元のスロットに存在するもの」として扱い、計算を行う補正処理を含んでいる。
/// </remarks>
public class MeterManager : MonoBehaviour
{
    [Header("Meters")]
    /// <summary>
    /// 知識量（正解アイテム数）を表示するメーターへの参照。
    /// </summary>
    public RetroMeter knowledgeMeter;

    /// <summary>
    /// 自律性（不正解アイテム数）を表示するメーターへの参照。
    /// </summary>
    public RetroMeter autonomyMeter;

    /// <summary>
    /// 完成度（全体の進捗状況）を表示するメーターへの参照。
    /// </summary>
    public RetroMeter completionMeter;

    [Header("References")]
    /// <summary>
    /// シーン内の全スロットを管理するマネージャー。
    /// </summary>
    public ObjectSlotManager slotManager;

    /// <summary>
    /// ゲームプレイの有効範囲となる画面領域（RawImage等）。
    /// この領域外にカーソルが出た場合のみ、アイテムが除去されたと判定しメーターを変動させる。
    /// </summary>
    [Tooltip("有効範囲となる画面領域。この領域外にカーソルが出た場合のみ、アイテム除去として判定される。")]
    public RectTransform gameScreenRect;

    /// <summary>
    /// UI判定に使用するカメラ。
    /// CanvasのRenderModeがScreenSpace - Overlayの場合はnull、Cameraの場合は対象カメラを設定する。
    /// </summary>
    [Tooltip("UI判定用カメラ。CanvasのRenderModeがOverlayの場合はnullを設定。")]
    public Camera uiCamera;

    /// <summary>
    /// 初期化処理。各メーターの初期構築を行う。
    /// </summary>
    void Start()
    {
        if (knowledgeMeter != null) knowledgeMeter.InitializeMeter();
        if (autonomyMeter != null) autonomyMeter.InitializeMeter();
        if (completionMeter != null) completionMeter.InitializeMeter();
    }

    /// <summary>
    /// フレームごとの更新処理。
    /// </summary>
    void Update()
    {
        UpdateMeters();
    }

    /// <summary>
    /// スロットの配置状況を集計し、各メーターの表示を更新する。
    /// </summary>
    /// <remarks>
    /// ドラッグ操作中のアイテムについては、カーソルが画面内にある限り
    /// 「元のスロットに配置されている」ものとして計算に含めることで、
    /// 操作中の意図しないメーター変動（フリッカー）を防止している。
    /// </remarks>
    private void UpdateMeters()
    {
        if (slotManager == null || slotManager.objectSlots == null) return;

        int totalSlots = slotManager.objectSlots.Count;
        if (totalSlots == 0) return;

        int knowledgeCount = 0;
        int autonomyCount = 0;

        // ドラッグ操作の状態取得
        var dragManager = DragDropManager.Instance;
        bool isHolding = dragManager != null && dragManager.IsHoldingItem;
        Draggable heldObject = dragManager != null ? dragManager.CurrentDraggedObject : null;
        ObjectSlot originalSlot = dragManager != null ? dragManager.OriginalSlot : null;

        // カーソルが有効領域内にあるか判定
        bool isCursorInsideScreen = false;
        if (gameScreenRect != null)
        {
            isCursorInsideScreen = RectTransformUtility.RectangleContainsScreenPoint(
                gameScreenRect,
                Input.mousePosition,
                uiCamera
            );
        }

        // 全スロットを走査してスコア計算
        foreach (var slot in slotManager.objectSlots)
        {
            ItemData dataToCheck = null;

            if (slot.IsOccupied() && slot.currentObject != null)
            {
                // 通常ケース：スロットにオブジェクトが実際に配置されている場合
                dataToCheck = slot.currentObject.itemData;
            }
            else if (isHolding && slot == originalSlot && heldObject != null && isCursorInsideScreen)
            {
                // ドラッグ中ケース（補正処理）：
                // スロットは空だが、ドラッグ中のオブジェクトの元位置であり、
                // かつカーソルが有効範囲内にある場合、このアイテムを含めて計算する
                dataToCheck = heldObject.itemData;
            }

            // アイテムデータが特定できた場合のみ集計
            if (dataToCheck != null)
            {
                if (slot.IsCorrectItem(dataToCheck.itemType))
                {
                    knowledgeCount++;
                }
                else
                {
                    autonomyCount++;
                }
            }
        }

        // --- 各メーターへの値の適用 ---

        // 知識量メーター更新（全体に対する正解数の割合）
        if (knowledgeMeter != null)
        {
            float ratio = (float)knowledgeCount / totalSlots;
            knowledgeMeter.UpdateMeterNormalized(ratio);
        }

        // 自律性メーター更新（全体に対する不正解数の割合）
        if (autonomyMeter != null)
        {
            float ratio = (float)autonomyCount / totalSlots;
            autonomyMeter.UpdateMeterNormalized(ratio);
        }

        // 完成度メーター更新
        // 計算式: (正解数 + (全体数 - 不正解数)) / (全体数 * 2)
        // ※空きスロットも「減点ではない」要素としてスコアに含まれる仕様
        if (completionMeter != null)
        {
            float maxScore = totalSlots * 2.0f;
            float currentScore = knowledgeCount + (totalSlots - autonomyCount);
            completionMeter.UpdateMeterNormalized(currentScore / maxScore);
        }
    }
}