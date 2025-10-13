using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Furniture : MonoBehaviour
{
    [Header("トリガー設定")]
    [Range(0, 100)]
    [Tooltip("NPCが触れた際に、家具へ向かう行動を誘発する確率(%)")]
    public float triggerProbability = 10f;

    private const string NpcFeetTag = "NPCFeet"; // NPCの足元を識別するためのタグ

    /// <summary>
    /// この家具のトリガーコライダーに何かが入ってきた時に呼び出される
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 接触してきたのがNPCの足元(NPCFeetタグ)かチェック
        if (other.CompareTag(NpcFeetTag))
        {
            // 2. 設定された確率で抽選を行う
            if (Random.Range(0f, 100f) < triggerProbability)
            {
                // 3. 接触してきたオブジェクトからNPCControllerを取得
                NPCController npc = other.GetComponentInParent<NPCController>();
                if (npc != null)
                {
                    // 4. NPCに行動をリクエストする
                    npc.RequestFurnitureInteraction();
                }
            }
        }
    }
}