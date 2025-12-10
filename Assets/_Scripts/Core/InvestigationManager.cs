using System;
using System.Collections.Generic;
using System.Linq; // LINQの拡張メソッド（Where, ToList）を使用するために必要
using UnityEngine;

/// <summary>
/// このシーン内での調査進行を管理するシングルトン。シーン単位で一つのインスタンスを想定。
/// </summary>
public class InvestigationManager : MonoBehaviour
{
    /// <summary>
    /// シーン内で有効なインスタンスへの参照。外部からは読み取りのみ許可。
    /// </summary>
    public static InvestigationManager Instance { get; private set; }

    /// <summary>
    /// Inspectorで設定する、この調査シーンで登場する可能性のある全ての証拠。D&Dで設定することを想定。
    /// </summary>
    [Tooltip("この調査シーンで登場する可能性のある全ての証拠")]
    public List<Clue> allCluesInThisScene;

    /// <summary>
    /// 証拠がアンロックされたときに発火するイベント。購読者へアンロックされたClueを渡す。
    /// </summary>
    public static event Action<Clue> OnClueUnlocked;

    /// <summary>
    /// MonoBehaviourのAwake。シーン内シングルトンの初期化を行う。重複インスタンスは破棄する。DontDestroyOnLoadは使用しない。
    /// </summary>
    private void Awake()
    {
        // シーン単位のシングルトン実装。重複時は警告を出して自身を破棄する。
        if (Instance != null)
        {
            Debug.LogWarning("InvestigationManagerがシーンに複数存在します。");
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        // シーン内シングルトンのため、オブジェクトはシーン切替で破棄される（__DontDestroyOnLoad__は使用しない）。
    }

    /// <summary>
    /// このシーンでアンロックされた証拠だけを抽出してGameManagerに渡す。GameManagerが見つからない場合は処理を中断してログ出力。
    /// </summary>
    public void PassCluesToGameManager()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerが見つかりません！");
            return;
        }

        // アンロック済みの証拠だけを抽出してList化（報告作成用）。
        List<Clue> unlockedClues = allCluesInThisScene.Where(clue => clue.isUnlocked).ToList();

        // GameManager側の収集リストに割り当てる（レポート作成時に使用）。
        GameManager.Instance.collectedCluesForReport = unlockedClues;

        Debug.Log($"{unlockedClues.Count}個の証拠をGameManagerに渡しました。");
    }

    /// <summary>
    /// 指定された証拠をアンロックする。引数がnullまたはこのシーンのリストに含まれていない場合は何もしない。
    /// 既にアンロック済みなら何もしない。アンロック時にイベントを発火する。
    /// </summary>
    /// <param name="clueToUnlock">アンロック対象のClue。Inspectorや他コンポーネントから渡されることを想定。</param>
    public void UnlockClue(Clue clueToUnlock)
    {
        if (clueToUnlock == null || !allCluesInThisScene.Contains(clueToUnlock)) return;
        if (clueToUnlock.isUnlocked) return;

        // 状態を変更してからイベント通知。購読者はUI更新やゲーム進行に反応する想定。
        clueToUnlock.isUnlocked = true;
        OnClueUnlocked?.Invoke(clueToUnlock);
        Debug.Log($"証拠をアンロックしました: {clueToUnlock.name}");
    }

    /// <summary>
    /// 指定された証拠がアンロック済みかを判定して返す。nullチェックあり。
    /// </summary>
    /// <param name="clueToCheck">判定対象のClue。</param>
    /// <returns>アンロック済みであればtrue、そうでなければfalse。</returns>
    public bool IsClueUnlocked(Clue clueToCheck)
    {
        return clueToCheck != null && clueToCheck.isUnlocked;
    }
}