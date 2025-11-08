using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EvaluationTrigger : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("シーン内のObjectSlotManager")]
    public ObjectSlotManager objectSlotManager;

    [Tooltip("スクリーンエフェクトを制御するコントローラ")]
    public ScreenEffectsController screenEffectsController;

    [Header("設定")]
    [Tooltip("遷移先のリザルトシーン名")]
    public string resultSceneName;

    [Tooltip("TVオフ演出の再生時間（秒）")]
    public float tvOffDelay = 2.0f;

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
            if (slot.isCorrectWhenEmpty)
            {
                if (!slot.IsOccupied())
                    score++;
            }
            else
            {
                if (slot.IsOccupied() && slot.currentObject.itemData.itemType == slot.correctItemType)
                    score++;
            }
        }

        GameManager.Instance.correctPlacementCount = score;
        GameManager.Instance.shouldShowResults = true;

        Debug.Log($"評価が完了。スコア: {score}。TVオフ演出を再生してリザルトシーンへ遷移します。");

        // 1. まずタスクバーを消す（予約）
        if (GlobalUIManager.Instance != null) GlobalUIManager.Instance.SetDesktopUIVisibility(false);

        // --- ▼ TVオフ演出とシーン遷移をコルーチンに任せる ▼ ---
        if (screenEffectsController != null)
        {
            StartCoroutine(DelayedSceneTransition()); // 遅延でシーン遷移
        }
        else
        {
            // 参照が無ければ即座に遷移
            SceneManager.LoadScene(resultSceneName);
        }
    }

    /// <summary>
    /// TVオフ演出が終わるのを待ってからシーンを切り替える
    /// </summary>
    private IEnumerator DelayedSceneTransition()
    {
        // 1. 1フレーム待機する
        yield return null;

        // 2. タスクバーが消えた後で、TVオフ演出を開始する
        if (screenEffectsController != null)
        {
            screenEffectsController.TriggerTvOff();
        }

        // 3. 元々の待機処理
        yield return new WaitForSeconds(tvOffDelay);
        SceneManager.LoadScene(resultSceneName);
    }
}
