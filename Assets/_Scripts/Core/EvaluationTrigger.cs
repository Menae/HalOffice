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

        // 1. スコアを計算する
        int score = 0;
        foreach (var slot in objectSlotManager.objectSlots)
        {
            // スロットに物があり、かつそのアイテムが正解のタイプと一致しているか？
            if (slot.IsOccupied() && slot.currentObject.itemData.itemType == slot.correctItemType)
            {
                score++;
            }
        }

        // 2. 結果をGameManagerに保存する
        GameManager.Instance.correctPlacementCount = score;
        GameManager.Instance.shouldShowResults = true; // 「合言葉」をtrueにする

        Debug.Log($"評価が完了。スコア: {score}。リザルトシーンへ遷移します。");

        // 3. リザルトシーンへ遷移する
        SceneManager.LoadScene(resultSceneName);
    }
}