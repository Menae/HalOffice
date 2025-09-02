using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DoubleClickButton : MonoBehaviour, IPointerClickHandler
{
    // Inspectorから設定するダブルクリック時のイベント
    public UnityEvent onDoubleClick;

    // クリックイベントを検知した際に呼ばれるメソッド
    public void OnPointerClick(PointerEventData eventData)
    {
        // クリックカウントが2回の場合（ダブルクリック）
        if (eventData.clickCount == 2)
        {
            // 設定されたイベントを実行する
            onDoubleClick.Invoke();
        }
    }
}