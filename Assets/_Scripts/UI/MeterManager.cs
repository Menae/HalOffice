using UnityEngine;

/// <summary>
/// スロット状況を監視し、RetroMeterコンポーネントを制御するクラス。
/// </summary>
public class MeterManager : MonoBehaviour
{
    [Header("Meters")]
    public RetroMeter knowledgeMeter;  // 知識量
    public RetroMeter autonomyMeter;   // 自律性
    public RetroMeter completionMeter; // 完成度

    [Header("References")]
    public ObjectSlotManager slotManager;

    void Start()
    {
        // ゲーム開始時にメーターのグリッドを生成させる
        if (knowledgeMeter != null) knowledgeMeter.InitializeMeter();
        if (autonomyMeter != null) autonomyMeter.InitializeMeter();
        if (completionMeter != null) completionMeter.InitializeMeter();
    }

    void Update()
    {
        UpdateMeters();
    }

    private void UpdateMeters()
    {
        if (slotManager == null || slotManager.objectSlots == null) return;

        int totalSlots = slotManager.objectSlots.Count;
        if (totalSlots == 0) return;

        int knowledgeCount = 0;
        int autonomyCount = 0;

        foreach (var slot in slotManager.objectSlots)
        {
            if (slot.IsOccupied())
            {
                if (slot.currentObject != null && slot.currentObject.itemData != null)
                {
                    if (slot.IsCorrectItem(slot.currentObject.itemData.itemType))
                    {
                        knowledgeCount++;
                    }
                    else
                    {
                        autonomyCount++;
                    }
                }
            }
        }

        // --- 1. 知識量 ---
        if (knowledgeMeter != null)
        {
            // メーター自体の最大目盛りをスロット総数に合わせる場合
            // knowledgeMeter.maxSteps = totalSlots; 
            // ※RetroMeterのmaxStepsを固定（例:10）にしたい場合は、比率計算をして渡すか、
            // そのまま渡してRetroMeter側でmaxStepsをスロット数と同じに設定しておく。
            // ここでは「スロット数＝メモリ数」として動的に合わせる例を示す:
            if (knowledgeMeter.maxSteps != totalSlots)
            {
                knowledgeMeter.maxSteps = totalSlots;
                knowledgeMeter.InitializeMeter();
            }
            knowledgeMeter.UpdateMeter(knowledgeCount);
        }

        // --- 2. 自律性（不正解数） ---
        if (autonomyMeter != null)
        {
            if (autonomyMeter.maxSteps != totalSlots)
            {
                autonomyMeter.maxSteps = totalSlots;
                autonomyMeter.InitializeMeter();
            }
            autonomyMeter.UpdateMeter(autonomyCount);
        }

        // --- 3. 完成度 ---
        if (completionMeter != null)
        {
            float maxScore = totalSlots * 2.0f;
            float currentScore = knowledgeCount + (totalSlots - autonomyCount);

            // 割合(0.0～1.0)で渡す
            completionMeter.UpdateMeterNormalized(currentScore / maxScore);
        }
    }
}