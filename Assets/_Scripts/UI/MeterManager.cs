using UnityEngine;

/// <summary>
/// ゲーム内のスロット状況（アイテムの配置状態）を常時監視し、
/// 知識量・自律性・完成度の各メーター（RetroMeter）へ値を反映させるマネージャークラス。
/// </summary>
/// <remarks>
/// ユーザー体験を損なわないよう、アイテムのドラッグ＆ドロップ操作中のメーター変動を抑制する機能を実装。
/// また、各メーターが最大値（100%）に達した際に、特定の演出用オブジェクトを有効化する機能を持つ。
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

    [Header("Max Value Events")]
    [Tooltip("知識メーターが最大(100%)になった時に有効化されるオブジェクト")]
    public GameObject knowledgeMaxEffect;

    [Tooltip("自律性メーターが最大(100%)になった時に有効化されるオブジェクト")]
    public GameObject autonomyMaxEffect;

    [Tooltip("完成度メーターが最大(100%)になった時に有効化されるオブジェクト")]
    public GameObject completionMaxEffect;

    [Header("Settings - Custom Max Values")]
    [Tooltip("知識メーターの最大値（分母）。0の場合は「全スロット数」が自動適用されます。")]
    public int customMaxKnowledge = 0;

    [Tooltip("自律性メーターの最大値（分母）。0の場合は「全スロット数」が自動適用されます。")]
    public int customMaxAutonomy = 0;

    [Tooltip("完成度メーターの最大スコア（分母）。0の場合は「全スロット数 × 2」が自動適用されます。")]
    public float customMaxCompletion = 0f;

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
    /// CanvasのRenderModeがOverlayの場合はnullを設定。
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

        // エフェクトの初期状態を非表示にする
        if (knowledgeMaxEffect != null) knowledgeMaxEffect.SetActive(false);
        if (autonomyMaxEffect != null) autonomyMaxEffect.SetActive(false);
        if (completionMaxEffect != null) completionMaxEffect.SetActive(false);
    }

    /// <summary>
    /// フレームごとの更新処理。
    /// </summary>
    void Update()
    {
        UpdateMeters();
    }

    /// <summary>
    /// スロットの配置状況を集計し、各メーターの表示更新および最大値到達時のイベント制御を行う。
    /// </summary>
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
                dataToCheck = slot.currentObject.itemData;
            }
            else if (isHolding && slot == originalSlot && heldObject != null && isCursorInsideScreen)
            {
                dataToCheck = heldObject.itemData;
            }

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

        // --- 1. 知識量 (Knowledge) ---
        // カスタム設定があればそれを使用、なければ全スロット数を使用
        float kDenominator = (customMaxKnowledge > 0) ? customMaxKnowledge : totalSlots;
        float knowledgeRatio = Mathf.Clamp01((float)knowledgeCount / kDenominator);

        if (knowledgeMeter != null)
        {
            knowledgeMeter.UpdateMeterNormalized(knowledgeRatio);
        }
        if (knowledgeMaxEffect != null)
        {
            knowledgeMaxEffect.SetActive(knowledgeRatio >= 1.0f);
        }


        // --- 2. 自律性 (Autonomy) ---
        float aDenominator = (customMaxAutonomy > 0) ? customMaxAutonomy : totalSlots;
        float autonomyRatio = Mathf.Clamp01((float)autonomyCount / aDenominator);

        if (autonomyMeter != null)
        {
            autonomyMeter.UpdateMeterNormalized(autonomyRatio);
        }
        if (autonomyMaxEffect != null)
        {
            autonomyMaxEffect.SetActive(autonomyRatio >= 1.0f);
        }


        // --- 3. 完成度 (Completion) ---
        // 現在のスコア計算: 正解数 + (空きスロット数) ※空きスロット数 = 全スロット - 不正解数
        // つまり「減点されなかった要素の数」
        float currentScore = knowledgeCount + (totalSlots - autonomyCount);

        // カスタム設定があればそれを使用、なければ標準計算(全スロット*2)を使用
        float cDenominator = (customMaxCompletion > 0f) ? customMaxCompletion : (totalSlots * 2.0f);
        float completionRatio = Mathf.Clamp01(currentScore / cDenominator);

        if (completionMeter != null)
        {
            completionMeter.UpdateMeterNormalized(completionRatio);
        }
        if (completionMaxEffect != null)
        {
            completionMaxEffect.SetActive(completionRatio >= 1.0f);
        }
    }
}