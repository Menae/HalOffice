using TMPro;
using UnityEngine;

public class ClueButton : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("このボタンに対応する証拠アセット")]
    public Clue associatedClue;
    [Tooltip("テキストを表示するTextMeshProコンポーネント")]
    public TextMeshProUGUI buttonText;

    [Header("テキストの色設定")]
    [Tooltip("証拠がアンロックされている時のテキスト色")]
    public Color unlockedColor = Color.black;
    [Tooltip("証拠が未調査（ロック）の時のテキスト色")]
    public Color lockedColor = Color.red;

    void Start()
    {
        // GameManagerが存在するか確認
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerが見つかりません！");
            buttonText.text = "ERROR";
            return;
        }

        // GameManagerが運んできた「発見済みの証拠リスト」に、このボタンの証拠が含まれているか確認
        if (GameManager.Instance.collectedCluesForReport != null &&
            GameManager.Instance.collectedCluesForReport.Contains(associatedClue))
        {
            // 含まれていれば、アンロック済みとして表示
            buttonText.text = associatedClue.description;
            buttonText.color = unlockedColor;
        }
        else
        {
            // 含まれていなければ、未調査として表示
            buttonText.text = "未調査";
            buttonText.color = lockedColor;
        }
    }
}