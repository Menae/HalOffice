// ファイル名: LoginSceneController.cs
using TMPro;
using UnityEngine;

public class LoginSceneController : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("日付を表示するTextMeshProコンポーネント")]
    public TextMeshProUGUI dayCounterText;

    void Start()
    {
        if (dayCounterText == null)
        {
            Debug.LogWarning("日付表示用のテキストが設定されていません。");
            return;
        }

        // GameManagerが存在し、かつ2日目以降の場合のみ日付を表示する
        if (GameManager.Instance != null && GameManager.Instance.currentDay > 1)
        {
            dayCounterText.gameObject.SetActive(true);
            dayCounterText.text = $"Day {GameManager.Instance.currentDay}";
        }
        else
        {
            // Day 1の場合やGameManagerがない場合は非表示にする
            dayCounterText.gameObject.SetActive(false);
        }
    }
}