using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class CursorController : MonoBehaviour
{
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

    [Header("効果音設定")]
    [Tooltip("左クリックした時に鳴らす効果音")]
    public AudioClip leftClickSound;
    [Range(0f, 1f)]
    [Tooltip("左クリック効果音の音量")]
    public float leftClickVolume = 1.0f;

    [Tooltip("右クリックした時に鳴らす効果音")]
    public AudioClip rightClickSound;
    [Range(0f, 1f)]
    [Tooltip("右クリック効果音の音量")]
    public float rightClickVolume = 1.0f;

    public Vector3 currentCursorPosition; // ゲーム内カーソルの現在位置

    // --- 内部変数 ---
    private Vector3 cursorVelocity = Vector3.zero; // SmoothDampで使う速度変数
    private AudioSource audioSource;
    private bool wasOverGameWorldLastFrame = false;

    private void OnEnable()
    {
        // 自分が有効になったことをDragDropManagerに登録する
        DragDropManager.RegisterCursor(this);
    }

    private void OnDisable()
    {
        // 自分が無効になることをDragDropManagerに登録解除する
        DragDropManager.UnregisterCursor(this);
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // ▼▼▼ この行を追加

        Cursor.visible = false;
        if (cursorImage == null) { Debug.LogError("Cursor Imageがセットされていません！"); return; }
        cursorImage.raycastTarget = false;
        SetCursorStateNormal();

        currentCursorPosition = Input.mousePosition;
    }

    void Update()
    {
        if (cursorImage == null || targetNpc == null) return;

        bool isOverGameWorld = IsPointerOverGameWorld();
        Vector3 targetPosition = (Vector2)Input.mousePosition + cursorOffset;
        cursorImage.enabled = true;

        if (targetNpc.isCursorInView && isOverGameWorld)
        {
            if (detectionIncreaseChannel != null)
            {
                float finalDetectionRate = GetCurrentDetectionMultiplier();
                detectionIncreaseChannel.RaiseEvent(finalDetectionRate * Time.deltaTime);
            }
            float currentShakeMagnitude = GetCurrentShakeMagnitude();
            Vector2 shakeOffset = Random.insideUnitCircle * currentShakeMagnitude;
            targetPosition += (Vector3)shakeOffset;
        }
        else
        {
            SetCursorStateNormal();
        }

        // 状態に応じてカーソルの移動方法を決定する
        if (isOverGameWorld)
        {
            // 1. ゲーム世界の上にいる場合：常に慣性をかける
            currentCursorPosition = Vector3.SmoothDamp(currentCursorPosition, targetPosition, ref cursorVelocity, smoothTime);
        }
        else // 2. UI要素の上にいる場合
        {
            // 2a. 前のフレームではゲーム世界にいた場合（＝UIに侵入した瞬間）
            if (wasOverGameWorldLastFrame)
            {
                // 視覚カーソルが実カーソルに追いつくまで、スムーズに着地させる
                if (Vector3.Distance(currentCursorPosition, targetPosition) > 1.0f)
                {
                    currentCursorPosition = Vector3.SmoothDamp(currentCursorPosition, targetPosition, ref cursorVelocity, smoothTime);
                }
                else // 追いついたら即座に追従
                {
                    currentCursorPosition = targetPosition;
                    cursorVelocity = Vector3.zero;
                }
            }
            // 2b. 前のフレームからずっとUIの上にいる場合
            else
            {
                // 慣性を完全に切り、即座に追従させる
                currentCursorPosition = targetPosition;
                cursorVelocity = Vector3.zero;
            }
        }

        // 最後に、現在の状態を「前のフレームの状態」として記憶する
        wasOverGameWorldLastFrame = isOverGameWorld;

        // クリック効果音の再生
        if (Input.GetMouseButtonDown(0))
        {
            if (audioSource != null && leftClickSound != null)
                audioSource.PlayOneShot(leftClickSound, leftClickVolume);
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (audioSource != null && rightClickSound != null)
                audioSource.PlayOneShot(rightClickSound, rightClickVolume);
        }
    }

    private void LateUpdate()
    {
        // 最終的なカーソル位置の適用は、全ての計算が終わったLateUpdateで行う
        if (cursorImage != null)
        {
            cursorImage.rectTransform.position = currentCursorPosition;
        }
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

    /// <summary>
    /// マウスカーソルが現在「ゲーム世界（InputBridge）」の上にあるかどうかを判定する
    /// </summary>
    /// <returns>ゲーム世界の上にあればtrue、それ以外のUI要素の上ならfalse</returns>
    private bool IsPointerOverGameWorld()
    {
        var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // もし何かUI要素にヒットしていて、かつその一番手前のものがInputBridgeなら
        // 「ゲーム世界の上にいる」と判断する
        if (results.Count > 0 && results[0].gameObject.GetComponent<InputBridge>() != null)
        {
            return true;
        }

        // それ以外（何もヒットしない、または一番手前がInputBridgeではない）は
        // 全て「ゲーム世界以外のUI」の上と判断
        return false;
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