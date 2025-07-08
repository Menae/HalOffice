using UnityEngine;

public class GameManager : MonoBehaviour
{
    // この"Instance"を通じて、どこからでもGameManagerにアクセスできる
    public static GameManager Instance { get; private set; }

    // 入力状態を管理するスイッチ
    public bool isInputEnabled { get; private set; } = true;

    private void Awake()
    {
        // シーンに他にGameManagerがいたら、自分を破壊する（リーダーは一人だけ）
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // 外部から入力状態を変更するための命令
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }
}