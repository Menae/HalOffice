using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        // InvestigationManagerが存在するか確認
        if (InvestigationManager.Instance == null)
        {
            Debug.LogError("InvestigationManagerが見つかりません！");
            buttonText.text = "ERROR";
            return;
        }

        // 証拠がアンロックされているか確認して、テキストと色を更新
        if (InvestigationManager.Instance.IsClueUnlocked(associatedClue))
        {
            buttonText.text = associatedClue.description;
            buttonText.color = unlockedColor; // アンロック時の色を設定
        }
        else
        {
            buttonText.text = "未調査";
            buttonText.color = lockedColor; // 未調査時の色を設定
        }
    }
}