using UnityEngine;
using UnityEngine.UI; // UI.Imageを扱うために必要

public class CursorController : MonoBehaviour
{
    [Header("カーソルの設定")]
    [Tooltip("ゲーム内でカーソルとして表示するUI Image")]
    public Image cursorImage; // ← Texture2DからImageに変更

    [Header("監視対象のNPC")]
    [Tooltip("震えの判定に使いたいNPCのオブジェクト")]
    public NPCMove_v1 targetNpc;

    [Header("震えの強さ")]
    [Tooltip("数値が大きいほど、カーソルが激しく震えます")]
    public float shakeMagnitude = 5.0f;

    void Start()
    {
        // OSのデフォルトカーソルは非表示にする
        Cursor.visible = false;

        // カーソル画像がセットされていなければ、何もしない
        if (cursorImage == null)
        {
            Debug.LogError("Cursor Imageがセットされていません！");
            return;
        }

        // カーソル画像がクリックの邪魔をしないようにする
        cursorImage.raycastTarget = false;
    }

    void Update()
    {
        // カーソル画像がセットされていなければ、何もしない
        if (cursorImage == null) return;

        // 毎フレーム、UIカーソルをマウスの現在位置に移動させる
        Vector2 mousePosition = Input.mousePosition;
        cursorImage.rectTransform.position = mousePosition;

        // NPCがセットされており、かつその視界に入っているか判定
        if (targetNpc != null && targetNpc.isCursorInView)
        {
            // --- 視界内にいる場合：カーソルを震わせる ---

            // ランダムな方向と強さの「揺れ」を計算
            Vector2 shakeOffset = Random.insideUnitCircle * shakeMagnitude;

            // マウスの現在位置に「揺れ」を加えて、カーソルの最終的な位置を補正
            cursorImage.rectTransform.position = mousePosition + shakeOffset;
        }
        // 視界外にいる場合は、上で設定した通りのマウス位置のままになる
    }
}