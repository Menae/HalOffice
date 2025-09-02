using UnityEngine;
using UnityEngine.UI;

public class JudgementZone : MonoBehaviour
{
    [Header("色の設定")]
    [Tooltip("承認スタンプが押された時の色")]
    public Color approveColor = Color.green;
    [Tooltip("却下スタンプが置かれた時の色")]
    public Color rejectColor = Color.red;

    [Header("参照")]
    public JudgementSequenceManager sequenceManager;

    private Image backgroundImage;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
    }

    // DraggableStampから直接呼ばれるスタンプを押す処理
    public void ApplyStamp(bool isApprove)
    {
        // ▼▼▼ ここから修正 ▼▼▼

        // --- 1. まず、色を変える処理を実行 ---
        if (isApprove)
        {
            backgroundImage.color = approveColor;
            Debug.Log("承認スタンプが押されました！");
        }
        else
        {
            backgroundImage.color = rejectColor;
            Debug.Log("却下スタンプが押されました！");
        }

        // --- 2. 次に、Managerにスタンプの種類を伝える ---
        if (sequenceManager != null)
        {
            sequenceManager.OnStampApplied(isApprove);
        }
        else
        {
            Debug.LogError("JudgementSequenceManagerが設定されていません！");
        }

        // ▲▲▲ ここまで修正 ▲▲▲
    }

    // ApplyStampColorメソッドはApplyStampに統合されたため不要
}