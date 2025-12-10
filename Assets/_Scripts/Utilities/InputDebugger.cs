using System.Text;              // 文字列結合を効率化するために必要
using TMPro;                    // TextMeshProを扱うために必要
using UnityEngine;
using UnityEngine.EventSystems; // UIイベントを検知するために必要

public class InputDebugger : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IPointerDownHandler
{
    [Header("UI参照")]
    [Tooltip("結果を表示するためのTextMeshProUGUI")]
    public TextMeshProUGUI logText;

    private StringBuilder sb = new StringBuilder();
    private string eventLog = "No events yet.";

    void Update()
    {
        if (logText == null) return;

        // 毎フレーム、生の入力状態をクリアして更新。
        sb.Clear();
        sb.AppendLine("--- Raw Input State (Input class) ---");
        sb.AppendLine($"Left Button Down:  {Input.GetMouseButtonDown(0)}");
        sb.AppendLine($"Right Button Down: {Input.GetMouseButtonDown(1)}");
        sb.AppendLine($"Middle Button Down:{Input.GetMouseButtonDown(2)}");
        sb.AppendLine("------------------------------------");
        sb.AppendLine("--- UI Event Log (EventSystem) ---");
        sb.AppendLine(eventLog);
        sb.AppendLine("------------------------------------");

        logText.text = sb.ToString();
    }

    // マウスボタンを「押し込んだ」瞬間に呼ばれる
    public void OnPointerDown(PointerEventData eventData)
    {
        eventLog = $"OnPointerDown event fired with {eventData.button} button.";
        Debug.Log(eventLog);
    }

    // マウスボタンを「押し込んで、離した」瞬間に呼ばれる
    public void OnPointerClick(PointerEventData eventData)
    {
        eventLog = $"OnPointerClick event fired with {eventData.button} button.";
        Debug.Log(eventLog);
    }

    // ドラッグが「開始した」瞬間に呼ばれる
    public void OnBeginDrag(PointerEventData eventData)
    {
        eventLog = $"OnBeginDrag event fired with {eventData.button} button.";
        Debug.Log(eventLog);
    }
}