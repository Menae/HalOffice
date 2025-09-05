using UnityEngine;

// CreateAssetMenuを使用すると、右クリックメニューから簡単にこのアセットを作成できるようになる
[CreateAssetMenu(fileName = "New Clue", menuName = "MyGame/Clue")]
public class Clue : ScriptableObject
{
    [Tooltip("この証拠のユニークなID")]
    public string clueID;

    [Tooltip("メモ欄や調書に表示されるテキスト")]
    [TextArea(3, 5)]
    public string description;

    [Header("発見状況")]
    [Tooltip("この証拠が発見済みかどうか")]
    public bool isUnlocked;

    // ゲーム開始時に状態をリセットするためのメソッド
    public void ResetStatus()
    {
        isUnlocked = false;
    }
}