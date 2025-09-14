using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq; // Linqを使うために必要

public class InvestigationManager : MonoBehaviour
{
    // このシーン内でのみ有効なシングルトン
    public static InvestigationManager Instance { get; private set; }

    [Tooltip("この調査シーンで登場する可能性のある全ての証拠")]
    public List<Clue> allCluesInThisScene;

    public static event Action<Clue> OnClueUnlocked;

    private void Awake()
    {
        // シーン内シングルトンの実装
        if (Instance != null)
        {
            Debug.LogWarning("InvestigationManagerがシーンに複数存在します。");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoadを削除！
    }

    /// <summary>
    /// このシーンでアンロックされた証拠だけをGameManagerに渡す
    /// </summary>
    public void PassCluesToGameManager()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerが見つかりません！");
            return;
        }

        // isUnlockedがtrueになっている証拠だけを抽出してリスト化する
        List<Clue> unlockedClues = allCluesInThisScene.Where(clue => clue.isUnlocked).ToList();
        
        // GameManagerの「運び屋」変数に、抽出したリストを渡す
        GameManager.Instance.collectedCluesForReport = unlockedClues;

        Debug.Log($"{unlockedClues.Count}個の証拠をGameManagerに渡しました。");
    }

    // 指定された証拠をアンロックするメソッド
    public void UnlockClue(Clue clueToUnlock)
    {
        if (clueToUnlock == null || !allCluesInThisScene.Contains(clueToUnlock)) return;
        if (clueToUnlock.isUnlocked) return;

        clueToUnlock.isUnlocked = true;
        OnClueUnlocked?.Invoke(clueToUnlock);
        Debug.Log($"証拠をアンロックしました: {clueToUnlock.name}");
    }

    // 指定された証拠がアンロック済みか確認するメソッド
    public bool IsClueUnlocked(Clue clueToCheck)
    {
        return clueToCheck != null && clueToCheck.isUnlocked;
    }
}