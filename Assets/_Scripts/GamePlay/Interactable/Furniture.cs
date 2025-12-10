using UnityEngine;

[RequireComponent(typeof(Collider2D))]
/// <summary>
/// 家具との接触を検知して、確率に応じてNPCへ行動要求を送るコンポーネント。
/// インスペクタで確率を調整して、NPCの行動パターンに変化を与える。
/// </summary>
public class Furniture : MonoBehaviour
{
    /// <summary>
    /// NPCが触れた際に、家具へ向かう行動を誘発する確率(%)。
    /// Inspectorで調整。範囲は0〜100。
    /// </summary>
    [Header("トリガー設定")]
    [Range(0, 100)]
    [Tooltip("NPCが触れた際に、家具へ向かう行動を誘発する確率(%)")]
    public float triggerProbability = 10f;

    /// <summary>
    /// NPCの足元を識別するためのタグ。
    /// Collider2Dの当たり判定はこのタグでフィルタリングする。
    /// </summary>
    private const string NpcFeetTag = "NPCFeet";

    /// <summary>
    /// トリガーコライダーに他のCollider2Dが侵入した際に呼ばれる。
    /// Unityの物理イベント。トリガーとして設定したCollider2Dに対して、
    /// 「侵入したフレーム」で実行される。
    /// </summary>
    /// <param name="other">侵入してきたCollider2D。親階層からNPCを探索する。nullチェックあり。</param>
    /// <remarks>
    /// トリガー判定はタグでフィルタリングする。親階層から取得したNPCControllerが存在する場合のみ
    /// 行動要求を送信する。GetComponentInParentはコンポーネント未存在時にnullを返すため安全にチェックする。
    /// </remarks>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // NPCの足元タグかどうかを確認してフィルタリング。
        if (other.CompareTag(NpcFeetTag))
        {
            // 設定確率に基づいてランダム抽選を行う。
            if (Random.Range(0f, 100f) < triggerProbability)
            {
                // 侵入してきたオブジェクトの親階層からNPCControllerを取得し、存在する場合に要求を送る。
                NPCController npc = other.GetComponentInParent<NPCController>();
                if (npc != null)
                {
                    npc.RequestFurnitureInteraction();
                }
            }
        }
    }
}