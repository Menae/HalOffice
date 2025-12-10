using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ポインタの入退出に応じて指定したハイライトの表示状態を切り替えるコンポーネント。
/// ポインタが対象UI要素に入る/出るタイミングで視覚的な強調を行うためにActiveを切り替える。
/// </summary>
/// <remarks>
/// EventSystemと適切なRaycasterが必要。Inspectorで`highlight`を設定。未設定の場合はNullReferenceExceptionが発生する点に注意。
/// </remarks>
public class HighlightScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // InspectorでD&D
    /// <summary>
    /// ハイライト用のGameObject。Inspectorで割り当てる。表示/非表示の切り替え対象。
    /// </summary>
    public GameObject highlight;

    /// <summary>
    /// ポインタが要素に入ったタイミングで呼ばれる。ハイライトを表示する。
    /// </summary>
    /// <param name="eventData">ポインタイベント情報。ポインタ座標やボタン情報を含む。</param>
    /// <remarks>EventSystem経由で呼ばれる。`highlight`がnullだと例外発生。</remarks>
    public void OnPointerEnter(PointerEventData eventData)
    {
        highlight.SetActive(true);
    }

    /// <summary>
    /// ポインタが要素から出たタイミングで呼ばれる。ハイライトを非表示にする。
    /// </summary>
    /// <param name="eventData">ポインタイベント情報。ポインタ座標やボタン情報を含む。</param>
    /// <remarks>EventSystem経由で呼ばれる。`highlight`がnullだと例外発生。</remarks>
    public void OnPointerExit(PointerEventData eventData)
    {
        highlight.SetActive(false);
    }
}