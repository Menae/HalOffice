using UnityEngine;
using UnityEngine.UI; // UI.Imageを扱うために必要

public class CursorController : MonoBehaviour
{
    [Header("カーソルの設定")]
    [Tooltip("ゲーム内でカーソルとして表示するUI Image")]
    public Image cursorImage;

    [Header("通常時の状態")]
    [Tooltip("視界外にいる時のカーソルの色（白で元の色）")]
    public Color normalColor = Color.white;
    [Range(0.1f, 5f)]
    [Tooltip("通常時のカーソルの表示倍率")]
    public float normalScale = 0.5f;

    [Header("視界内での変化")]
    // --- 遠距離 ---
    [Tooltip("【遠距離】の色")]
    public Color farColor = Color.yellow;
    [Range(0.1f, 5f)]
    [Tooltip("【遠距離】の表示倍率")]
    public float farScale = 0.6f;
    [Range(0f, 5f)]
    [Tooltip("【遠距離】の震えの倍率")]
    public float farShakeMultiplier = 1.0f;

    // --- 中距離 ---
    [Tooltip("【中距離】の色")]
    public Color mediumColor = new Color(1.0f, 0.5f, 0f); // オレンジ色
    [Range(0.1f, 5f)]
    [Tooltip("【中距離】の表示倍率")]
    public float mediumScale = 0.7f;
    [Range(0f, 5f)]
    [Tooltip("【中距離】の震えの倍率")]
    public float mediumShakeMultiplier = 1.5f;
    [Tooltip("この距離より近いとオレンジ色になる境界線")]
    public float mediumDistanceThreshold = 4.0f;

    // --- 近距離 ---
    [Tooltip("【近距離】の色")]
    public Color closeColor = Color.red;
    [Range(0.1f, 5f)]
    [Tooltip("【近距離】の表示倍率")]
    public float closeScale = 0.8f;
    [Range(0f, 5f)]
    [Tooltip("【近距離】の震えの倍率")]
    public float closeShakeMultiplier = 2.5f;
    [Tooltip("この距離より近いと赤色になる境界線")]
    public float closeDistanceThreshold = 2.0f;

    [Header("監視対象のNPC")]
    [Tooltip("震えの判定に使いたいNPCのオブジェクト")]
    public NPCController targetNpc;

    // ★★★ 変更：基本の震えの強さを設定 ★★★
    [Header("震えの基本設定")]
    [Tooltip("震えの基本となる強さ。これに各距離の倍率が掛かる")]
    public float baseShakeMagnitude = 2.0f;

    void Start()
    {
        Cursor.visible = false;
        if (cursorImage == null) { Debug.LogError("Cursor Imageがセットされていません！"); return; }
        cursorImage.raycastTarget = false;
        cursorImage.color = normalColor;
        cursorImage.rectTransform.localScale = Vector3.one * normalScale;
    }

    void Update()
    {
        if (cursorImage == null) return;

        Vector2 mousePosition = Input.mousePosition;

        if (targetNpc != null && targetNpc.isCursorInView)
        {
            // --- 視界内にいる場合 ---
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            float distance = Vector3.Distance(targetNpc.transform.position, mouseWorldPos);

            // ★★★ 追加：現在の震えの強さを保持する変数を宣言 ★★★
            float currentShakeMagnitude = baseShakeMagnitude;

            if (distance < closeDistanceThreshold)
            {
                // 近距離の場合
                cursorImage.color = closeColor;
                cursorImage.rectTransform.localScale = Vector3.one * closeScale;
                // ★★★ 追加：近距離の倍率を適用 ★★★
                currentShakeMagnitude *= closeShakeMultiplier;
            }
            else if (distance < mediumDistanceThreshold)
            {
                // 中距離の場合
                cursorImage.color = mediumColor;
                cursorImage.rectTransform.localScale = Vector3.one * mediumScale;
                // ★★★ 追加：中距離の倍率を適用 ★★★
                currentShakeMagnitude *= mediumShakeMultiplier;
            }
            else
            {
                // 遠距離の場合
                cursorImage.color = farColor;
                cursorImage.rectTransform.localScale = Vector3.one * farScale;
                // ★★★ 追加：遠距離の倍率を適用 ★★★
                currentShakeMagnitude *= farShakeMultiplier;
            }

            // 最終的に計算された強さでカーソルを震わせる
            Vector2 shakeOffset = Random.insideUnitCircle * currentShakeMagnitude;
            cursorImage.rectTransform.position = mousePosition + shakeOffset;
        }
        else
        {
            // --- 視界外にいる場合 ---
            cursorImage.color = normalColor;
            cursorImage.rectTransform.localScale = Vector3.one * normalScale;
            cursorImage.rectTransform.position = mousePosition;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetNpc.transform.position.z - Camera.main.transform.position.z);
        mousePos.z = distanceFromCamera;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}