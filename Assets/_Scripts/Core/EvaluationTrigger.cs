// ファイル名: EvaluationTrigger.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class EvaluationTrigger : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーン内のObjectSlotManager")]
    public ObjectSlotManager objectSlotManager;

    [Header("設定")]
    [Tooltip("遷移先のリザルトシーン名")]
    public string resultSceneName;

    /// <summary>
    /// ボタンのOnClickイベントなどから呼び出すための公開メソッド
    /// </summary>
    public void EndDayAndEvaluate()
    {
        if (objectSlotManager == null)
        {
            Debug.LogError("ObjectSlotManagerが設定されていません！");
            return;
        }

        int score = 0;
        foreach (var slot in objectSlotManager.objectSlots)
        {
            // ケース1：このスロットは「空であること」が正解か？
            if (slot.isCorrectWhenEmpty)
            {
                if (!slot.IsOccupied()) // そして、実際に空か？
                {
                    score++; // 正解！
                }
            }
            // ケース2：このスロットは「特定のアイテムがあること」が正解か？
            else
            {
                if (slot.IsOccupied() && slot.currentObject.itemData.itemType == slot.correctItemType)
                {
                    score++; // 正解！
                }
            }
        }

        GameManager.Instance.correctPlacementCount = score;
        GameManager.Instance.shouldShowResults = true;

        Debug.Log($"評価が完了。スコア: {score}。リザルトシーンへ遷移します。");

        SceneManager.LoadScene(resultSceneName);
    }
}