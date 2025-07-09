using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// IDropHandlerはもう使わないので削除
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

    // ★★★ OnDropメソッドを削除し、新しい命令受け取り用のメソッドを追加 ★★★
    // DraggableStampから直接呼ばれる、スタンプを押す処理
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