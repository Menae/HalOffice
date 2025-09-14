using UnityEngine;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [Header("UI参照")]
    public Canvas parentCanvas;
    public Camera uiCamera;

    [Header("カーソルの設定")]
    public Image cursorImage;

    [Header("慣性設定")]
    [Tooltip("カーソルの追従の滑らかさ。値が小さいほど機敏に、大きいほど重くなる。")]
    [Range(0.01f, 0.5f)]
    public float smoothTime = 0.1f;

    [Header("カーソルの状態")]
    public Color normalColor = Color.white;
    [Range(0.1f, 5f)]
    public float normalScale = 0.5f;

    [Header("視界内での変化（距離別）")]
    public Color farColor = Color.yellow;
    [Range(0f, 5f)]
    public float farShakeMultiplier = 1.0f;
    [Range(0f, 10f)]
    public float farDetectionMultiplier = 1.0f;
    public Color mediumColor = new Color(1.0f, 0.5f, 0f);
    [Range(0f, 5f)]
    public float mediumShakeMultiplier = 1.5f;
    [Range(0f, 10f)]
    public float mediumDetectionMultiplier = 2.0f;
    public float mediumDistanceThreshold = 4.0f;
    public Color closeColor = Color.red;
    [Range(0f, 5f)]
    public float closeShakeMultiplier = 2.5f;
    [Range(0f, 10f)]
    public float closeDetectionMultiplier = 4.0f;
    public float closeDistanceThreshold = 2.0f;

    [Header("監視対象のNPC")]
    public NPCController targetNpc;

    [Header("震えの基本設定")]
    public float baseShakeMagnitude = 2.0f;

    [Header("イベント発行")]
    public FloatEventChannelSO detectionIncreaseChannel;
    public float detectionIncreaseRate = 10f;
    
    [Header("位置調整")]
    [Tooltip("カーソルの表示位置を微調整します (X:左右, Y:上下)")]
    public Vector2 cursorOffset;

    // --- 内部変数 ---
    private Vector3 currentCursorPosition; // ゲーム内カーソルの現在位置
    private Vector3 cursorVelocity = Vector3.zero; // SmoothDampで使う速度変数

    void Start()
    {
        Cursor.visible = false;
        if (cursorImage == null) { Debug.LogError("Cursor Imageがセットされていません！"); return; }
        cursorImage.raycastTarget = false;
        SetCursorStateNormal();

        // 起動時のカーソル位置を、現在のマウス位置で初期化
        currentCursorPosition = Input.mousePosition;
    }

    void Update()
    {
        if (cursorImage == null || uiCamera == null || parentCanvas == null || targetNpc == null) return;

        cursorImage.enabled = true;

        // 1. 「目標となる位置」を計算する (実際のOSマウスカーソルの位置)
        Vector3 targetPosition = (Vector2)Input.mousePosition + cursorOffset;

        // 2. 視界内に入っているかチェックし、見た目とイベント発行を処理
        if (targetNpc.isCursorInView)
        {
            // 見つかり度を上昇させる
            if (detectionIncreaseChannel != null)
            {
                float finalDetectionRate = GetCurrentDetectionMultiplier();
                detectionIncreaseChannel.RaiseEvent(finalDetectionRate * Time.deltaTime);
            }

            // 震えの演出を「目標位置」に加える
            float currentShakeMagnitude = GetCurrentShakeMagnitude();
            Vector2 shakeOffset = Random.insideUnitCircle * currentShakeMagnitude;
            targetPosition += (Vector3)shakeOffset;
        }
        else
        {
            // 視界外なら通常状態に戻す
            SetCursorStateNormal();
        }

        // 3. ゲーム内カーソルの現在位置を、目標位置に向かって滑らかに移動させる
        currentCursorPosition = Vector3.SmoothDamp(currentCursorPosition, targetPosition, ref cursorVelocity, smoothTime);

        // 4. 最終的なカーソル位置を、滑らかに移動した後の位置を使って適用する
        Vector3 screenPosWithZ = currentCursorPosition;
        screenPosWithZ.z = parentCanvas.planeDistance;
        cursorImage.rectTransform.position = uiCamera.ScreenToWorldPoint(screenPosWithZ);
    }

    // 視界内にいる時の見た目の変化を適用し、震えの強さを返すメソッド
    private float GetCurrentShakeMagnitude()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        float distance = Vector3.Distance(targetNpc.transform.position, mouseWorldPos);

        // 視界内では大きさは常に一定
        cursorImage.rectTransform.localScale = Vector3.one * normalScale;

        if (distance < closeDistanceThreshold)
        {
            cursorImage.color = closeColor;
            return baseShakeMagnitude * closeShakeMultiplier;
        }
        else if (distance < mediumDistanceThreshold)
        {
            cursorImage.color = mediumColor;
            return baseShakeMagnitude * mediumShakeMultiplier;
        }
        else
        {
            cursorImage.color = farColor;
            return baseShakeMagnitude * farShakeMultiplier;
        }
    }

    // 視界内にいる時の見つかり度上昇倍率を返すメソッド
    private float GetCurrentDetectionMultiplier()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        float distance = Vector3.Distance(targetNpc.transform.position, mouseWorldPos);

        if (distance < closeDistanceThreshold)
        {
            return detectionIncreaseRate * closeDetectionMultiplier;
        }
        else if (distance < mediumDistanceThreshold)
        {
            return detectionIncreaseRate * mediumDetectionMultiplier;
        }
        else
        {
            return detectionIncreaseRate * farDetectionMultiplier;
        }
    }

    // カーソルを通常状態に戻す処理をまとめたメソッド
    private void SetCursorStateNormal()
    {
        cursorImage.color = normalColor;
        cursorImage.rectTransform.localScale = Vector3.one * normalScale;
    }

    private Vector3 GetMouseWorldPosition()
    {
        // ScreenToWorldConverterがインスタンス化されており、かつ座標を取得できた場合
        if (ScreenToWorldConverter.Instance != null && 
            ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
        {
            // 正しいワールド座標を返す
            return worldPos;
        }

        // カーソルがゲーム画面の外にあるなど、座標が取得できなかった場合
        // 非常に遠い座標を返すことで、NPCが決して反応しないようにする
        return new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    }
}