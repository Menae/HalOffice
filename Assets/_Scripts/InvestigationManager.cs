using System.Collections.Generic;
using UnityEngine;
using System; // Actionを使うために必要

public class InvestigationManager : MonoBehaviour
{
    public static InvestigationManager Instance { get; private set; }

    // 全ての証拠データを保持するリスト
    public List<Clue> allClues;

    // 証拠がアンロックされた時に他のスクリプトに通知するためのイベント
    public static event Action<Clue> OnClueUnlocked;

    private void Awake()
    {
        // シーンをまたいで存在し続けるシングルトンの実装
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetAllClues(); // ゲーム開始時に全ての証拠を未発見状態にする
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 全ての証拠をリセットする
    public void ResetAllClues()
    {
        foreach (var clue in allClues)
        {
            clue.ResetStatus();
        }
    }

    // 指定された証拠をアンロックするメソッド
    public void UnlockClue(Clue clueToUnlock)
    {
        if (clueToUnlock == null) return;

        // ▼▼▼ デバッグログを追加 ▼▼▼
        // どの証拠をアンロックしようとしているか、そのインスタンスIDと名前、現在の状態を出力
        Debug.Log($"UnlockClueが呼ばれました。対象: [{clueToUnlock.GetInstanceID()}] {clueToUnlock.name}, 現在のisUnlocked: {clueToUnlock.isUnlocked}");

        // すでにアンロック済みなら、ここで処理を終了
        if (clueToUnlock.isUnlocked) return;

        clueToUnlock.isUnlocked = true;

        // 変更後の状態をログに出力
        Debug.Log($"===> 結果: [{clueToUnlock.GetInstanceID()}] {clueToUnlock.name} の isUnlocked を true に設定しました。");

        // 証拠がアンロックされたことを通知
        OnClueUnlocked?.Invoke(clueToUnlock);
    }

    // 指定された証拠がアンロック済みか確認するメソッド
    public bool IsClueUnlocked(Clue clueToCheck)
    {
        return clueToCheck != null && clueToCheck.isUnlocked;
    }
}