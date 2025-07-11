using UnityEngine;
using UnityEngine.UI;

public class JudgementZone : MonoBehaviour
{
    [Header("色の設定")]
    [Tooltip("承認スタンプが押された時の色")]
    public Color approveColor = Color.green;
    [Tooltip("却下スタンプが置かれた時の色")]
    public Color rejectColor = Color.red;

    private Image backgroundImage;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
    }

    //命令受け取り用のメソッド
    //DraggableStampから直接呼ばれるスタンプを押す処理
    public void ApplyStamp(bool isApprove)
    {
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
    }
}