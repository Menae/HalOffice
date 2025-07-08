using UnityEngine;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [Header("UI参照")]
    public Canvas parentCanvas;
    public Camera uiCamera;

    [Header("カーソルの設定")]
    public Image cursorImage;

    [Header("カーソルの状態")]
    public Color normalColor = Color.white;
    [Range(0.1f, 5f)]
    public float normalScale = 0.5f;

    [Header("視界内での変化（距離別）")]
    // --- 遠距離 ---
    [Tooltip("【遠距離】の色")]
    public Color farColor = Color.yellow;
    [Range(0f, 5f)]
    [Tooltip("【遠距離】の震えの倍率")]
    public float farShakeMultiplier = 1.0f;
    [Range(0f, 10f)]
    [Tooltip("【遠距離】の見つかり度上昇倍率")]
    public float farDetectionMultiplier = 1.0f;

    // --- 中距離 ---
    [Tooltip("【中距離】の色")]
    public Color mediumColor = new Color(1.0f, 0.5f, 0f); // オレンジ色
    [Range(0f, 5f)]
    [Tooltip("【中距離】の震えの倍率")]
    public float mediumShakeMultiplier = 1.5f;
    [Range(0f, 10f)]
    [Tooltip("【中距離】の見つかり度上昇倍率")]
    public float mediumDetectionMultiplier = 2.0f;
    [Tooltip("この距離より近いとオレンジ色になる境界線")]
    public float mediumDistanceThreshold = 4.0f;

    // --- 近距離 ---
    [Tooltip("【近距離】の色")]
    public Color closeColor = Color.red;
    [Range(0f, 5f)]
    [Tooltip("【近距離】の震えの倍率")]
    public float closeShakeMultiplier = 2.5f;
    [Range(0f, 10f)]
    [Tooltip("【近距離】の見つかり度上昇倍率")]
    public float closeDetectionMultiplier = 4.0f;
    [Tooltip("この距離より近いと赤色になる境界線")]
    public float closeDistanceThreshold = 2.0f;

    [Header("監視対象のNPC")]
    public NPCController targetNpc;

    [Header("震えの基本設定")]
    [Tooltip("震えの基本となる強さ。これに各距離の倍率が掛かる")]
    public float baseShakeMagnitude = 2.0f;

    [Header("イベント発行")]
    public FloatEventChannelSO detectionIncreaseChannel;
    [Tooltip("見つかり度が1秒あたりに上昇する基本量")]
    public float detectionIncreaseRate = 10f; // 基本量として分かりやすく変更

    void Start()
    {
        Cursor.visible = false;
        if (cursorImage == null) { Debug.LogError("Cursor Imageがセットされていません！"); return; }
        cursorImage.raycastTarget = false;
        SetCursorStateNormal();
    }

    void Update()
    {
        if (cursorImage == null || uiCamera == null || parentCanvas == null) return;

        // --- 1. カーソルの基本位置と見た目を設定 ---
        Vector2 finalScreenPosition = Input.mousePosition;
        cursorImage.enabled = true;
        cursorImage.color = normalColor;
        cursorImage.rectTransform.localScale = Vector3.one * normalScale;

        // --- 2. ゲームの状態に応じて処理を分岐 ---
        if (GameManager.Instance != null && GameManager.Instance.isInputEnabled)
        {
            // --- 入力が有効な場合（ゲームプレイ中）---
            if (targetNpc != null && targetNpc.isCursorInView)
            {
                // --- 視界内にいる場合 ---
                float currentShakeMultiplier = 1f;
                float currentDetectionMultiplier = 1f;

                Vector3 mouseWorldPos = GetMouseWorldPosition();
                float distance = Vector3.Distance(targetNpc.transform.position, mouseWorldPos);

                // 距離に応じて色と各種倍率を決定
                if (distance < closeDistanceThreshold)
                {
                    cursorImage.color = closeColor;
                    currentShakeMultiplier = closeShakeMultiplier;
                    currentDetectionMultiplier = closeDetectionMultiplier;
                }
                else if (distance < mediumDistanceThreshold)
                {
                    cursorImage.color = mediumColor;
                    currentShakeMultiplier = mediumShakeMultiplier;
                    currentDetectionMultiplier = mediumDetectionMultiplier;
                }
                else
                {
                    cursorImage.color = farColor;
                    currentShakeMultiplier = farShakeMultiplier;
                }

                // 震えを適用
                float finalShakeMagnitude = baseShakeMagnitude * currentShakeMultiplier;
                Vector2 shakeOffset = Random.insideUnitCircle * finalShakeMagnitude;
                finalScreenPosition += shakeOffset;

                // 見つかり度を上昇させるイベントを発行
                if (detectionIncreaseChannel != null)
                {
                    float finalDetectionRate = detectionIncreaseRate * currentDetectionMultiplier;
                    detectionIncreaseChannel.RaiseEvent(finalDetectionRate * Time.deltaTime);
                }
            }
        }

        // --- 3. 最終的なカーソル位置を適用 ---
        Vector3 screenPosWithZ = finalScreenPosition;
        screenPosWithZ.z = parentCanvas.planeDistance;
        cursorImage.rectTransform.position = uiCamera.ScreenToWorldPoint(screenPosWithZ);
    }

    private void SetCursorStateNormal()
    {
        cursorImage.enabled = true;
        cursorImage.color = normalColor;
        cursorImage.rectTransform.localScale = Vector3.one * normalScale;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (targetNpc == null) return Vector3.zero;
        Vector3 mousePos = Input.mousePosition;
        float distanceFromCamera = Mathf.Abs(targetNpc.transform.position.z - Camera.main.transform.position.z);
        mousePos.z = distanceFromCamera;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}