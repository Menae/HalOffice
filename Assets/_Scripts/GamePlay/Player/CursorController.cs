using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// カーソルの表示と挙動を管理するコンポーネント。
/// 視界内での震え・色変化・NPCへの検出値送出と、クリック音の再生を担当する。
/// AudioSource必須。Inspectorでの参照設定に依存する箇所はnullチェックあり。
/// </summary>
public class CursorController : MonoBehaviour
{
    [Header("カーソルの設定")]
    /// <summary>
    /// InspectorでD&D。画面上に表示するカーソルのImage参照。null時はStartでエラーログと早期リターン。
    /// </summary>
    public Image cursorImage;

    [Header("慣性設定")]
    [Tooltip("カーソルの追従の滑らかさ。値が小さいほど機敏に、大きいほど重くなる。")]
    [Range(0.01f, 0.5f)]
    /// <summary>
    /// カーソル追従の滑らかさ（SmoothDampの時間）。小さいほど即時追従に近づく。
    /// </summary>
    public float smoothTime = 0.1f;

    [Header("カーソルの状態")]
    /// <summary>通常時の色。</summary>
    public Color normalColor = Color.white;
    [Range(0.1f, 5f)]
    /// <summary>通常時のスケール。rectTransformに適用。</summary>
    public float normalScale = 0.5f;

    [Header("視界内での変化（距離別）")]
    /// <summary>遠距離時の色。</summary>
    public Color farColor = Color.yellow;
    [Range(0f, 5f)]
    /// <summary>遠距離時の震え倍率。baseShakeMagnitudeに乗算される。</summary>
    public float farShakeMultiplier = 1.0f;
    [Range(0f, 10f)]
    /// <summary>遠距離時の発見倍率。detectionIncreaseRateに乗算される。</summary>
    public float farDetectionMultiplier = 1.0f;
    /// <summary>中距離時の色。</summary>
    public Color mediumColor = new Color(1.0f, 0.5f, 0f);
    [Range(0f, 5f)]
    /// <summary>中距離時の震え倍率。</summary>
    public float mediumShakeMultiplier = 1.5f;
    [Range(0f, 10f)]
    /// <summary>中距離時の発見倍率。</summary>
    public float mediumDetectionMultiplier = 2.0f;
    /// <summary>中距離と遠距離の境界（ワールド単位）。</summary>
    public float mediumDistanceThreshold = 4.0f;
    /// <summary>近距離時の色。</summary>
    public Color closeColor = Color.red;
    [Range(0f, 5f)]
    /// <summary>近距離時の震え倍率。</summary>
    public float closeShakeMultiplier = 2.5f;
    [Range(0f, 10f)]
    /// <summary>近距離時の発見倍率。</summary>
    public float closeDetectionMultiplier = 4.0f;
    /// <summary>近距離と中距離の境界（ワールド単位）。</summary>
    public float closeDistanceThreshold = 2.0f;

    [Header("監視対象のNPC")]
    /// <summary>InspectorでD&D。監視対象のNPCController。nullチェックあり。</summary>
    public NPCController targetNpc;

    [Header("震えの基本設定")]
    /// <summary>震えの基本振幅。shakeMultiplierと乗算して使用。</summary>
    public float baseShakeMagnitude = 2.0f;

    [Header("イベント発行")]
    /// <summary>検出値増加を通知するイベントチャンネル。Inspectorで割当てる。nullチェックあり。</summary>
    public FloatEventChannelSO detectionIncreaseChannel;
    /// <summary>基本の検出値上昇レート（秒あたり）。</summary>
    public float detectionIncreaseRate = 10f;

    [Header("位置調整")]
    [Tooltip("カーソルの表示位置を微調整します (X:左右, Y:上下)")]
    /// <summary>画面上でのカーソル位置オフセット。UI配置調整用（ピクセル単位）。</summary>
    public Vector2 cursorOffset;

    [Header("効果音設定")]
    [Tooltip("左クリックした時に鳴らす効果音")]
    /// <summary>左クリック時に再生するAudioClip。nullチェックあり。</summary>
    public AudioClip leftClickSound;
    [Range(0f, 1f)]
    [Tooltip("左クリック効果音の音量")]
    /// <summary>左クリック効果音の音量（0-1）。</summary>
    public float leftClickVolume = 1.0f;

    [Tooltip("右クリックした時に鳴らす効果音")]
    /// <summary>右クリック時に再生するAudioClip。nullチェックあり。</summary>
    public AudioClip rightClickSound;
    [Range(0f, 1f)]
    [Tooltip("右クリック効果音の音量")]
    /// <summary>右クリック効果音の音量（0-1）。</summary>
    public float rightClickVolume = 1.0f;

    /// <summary>ゲーム内カーソルの現在位置（スクリーン座標）。Updateで更新し、LateUpdateで適用。</summary>
    public Vector3 currentCursorPosition; // ゲーム内カーソルの現在位置

    // --- 内部変数 ---
    private Vector3 cursorVelocity = Vector3.zero; // SmoothDampで使う速度変数
    private AudioSource audioSource;
    private bool wasOverGameWorldLastFrame = false;
    private bool isInputEnabled = true;

    /// <summary>
    /// 有効化時にDragDropManagerへ自身を登録する。Startより先に呼ばれる場合あり。
    /// </summary>
    private void OnEnable()
    {
        DragDropManager.RegisterCursor(this);
    }

    /// <summary>
    /// 無効化時にDragDropManagerから自身の登録を解除する。
    /// </summary>
    private void OnDisable()
    {
        DragDropManager.UnregisterCursor(this);
    }

    /// <summary>
    /// Unity Start。最初のフレーム直前に一度だけ呼ばれる。
    /// AudioSource取得・OSカーソル非表示・cursorImageの初期設定・初期位置設定を実施。
    /// cursorImageが未設定の場合はエラーログ出力して処理を中断。
    /// </summary>
    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // AudioSource取得（クリック音再生用）

        Cursor.visible = false;
        if (cursorImage == null) { Debug.LogError("Cursor Imageがセットされていません！"); return; }
        cursorImage.raycastTarget = false;
        SetCursorStateNormal();

        currentCursorPosition = Input.mousePosition;
    }

    /// <summary>
    /// 外部から入力を有効/無効にする。
    /// 無効化時は即座に見た目を通常状態に戻す。
    /// </summary>
    /// <param name="enabled">入力を許可するならtrue、無効化するならfalse。</param>
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
        if (!enabled)
        {
            SetCursorStateNormal(); // 見た目を通常に戻す
        }
    }

    /// <summary>
    /// Unity Update。毎フレーム呼ばれ、NPCとの干渉判定・カーソル移動・クリック音再生を行う。
    /// 入力が無効でもクリック音は再生してフィードバックを提供。
    /// targetNpcやcursorImageがnullの場合は早期リターン。
    /// </summary>
    void Update()
    {
        if (cursorImage == null || targetNpc == null) return;

        bool isOverGameWorld = IsPointerOverGameWorld();
        Vector3 targetPosition = (Vector2)Input.mousePosition + cursorOffset;
        cursorImage.enabled = true;

        // --- 1. NPCへの干渉（視線検知・震え） ---
        // 入力が有効かつNPC視界内かつゲームワールド上にいる場合のみ実行
        if (isInputEnabled && targetNpc.isCursorInView && isOverGameWorld)
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

        // --- 2. カーソルの移動 ---
        // 常に位置更新処理を行い、見た目がフリーズした印象とならないようにする
        if (isOverGameWorld)
        {
            currentCursorPosition = Vector3.SmoothDamp(currentCursorPosition, targetPosition, ref cursorVelocity, smoothTime);
        }
        else
        {
            // UI上の挙動：直前がゲームワールド上だった場合は慣性を保持して滑らかに遷移
            if (wasOverGameWorldLastFrame)
            {
                if (Vector3.Distance(currentCursorPosition, targetPosition) > 1.0f)
                    currentCursorPosition = Vector3.SmoothDamp(currentCursorPosition, targetPosition, ref cursorVelocity, smoothTime);
                else
                {
                    currentCursorPosition = targetPosition;
                    cursorVelocity = Vector3.zero;
                }
            }
            else
            {
                currentCursorPosition = targetPosition;
                cursorVelocity = Vector3.zero;
            }
        }
        wasOverGameWorldLastFrame = isOverGameWorld;

        // --- 3. クリック音の再生 ---
        // 入力が無効でもクリック音は鳴らす（フィードバック目的）
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

    /// <summary>
    /// Unity LateUpdate。全ての計算が終わった後に表示位置を適用する。
    /// rectTransformへの直接代入を行うため、Updateでの位置計算と分離。
    /// </summary>
    private void LateUpdate()
    {
        if (cursorImage != null)
        {
            cursorImage.rectTransform.position = currentCursorPosition;
        }
    }

    /// <summary>
    /// 視界内にいる時の見た目の変化を適用し、現在の震えの強さを返す。
    /// GetMouseWorldPositionが失敗すると非常に遠い座標が返るため、その場合は遠距離扱いになる。
    /// </summary>
    /// <returns>現在の震え振幅（ピクセル単位）。</returns>
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

    /// <summary>
    /// 視界内にいる時の見つかり度上昇倍率を返す。
    /// GetMouseWorldPositionが正常でない場合は遠距離向けの倍率を返す。
    /// </summary>
    /// <returns>秒あたりの検出増加量。</returns>
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

    /// <summary>
    /// カーソルを通常状態に戻す処理をまとめる。色とスケールを通常値に設定。
    /// nullチェックは呼び出し側で行うこと。
    /// </summary>
    private void SetCursorStateNormal()
    {
        cursorImage.color = normalColor;
        cursorImage.rectTransform.localScale = Vector3.one * normalScale;
    }

    /// <summary>
    /// マウスカーソルが現在「ゲーム世界（InputBridge）」の上にあるか判定する。
    /// EventSystemを使ったUIレイキャストを行い、最前面のヒットオブジェクトがInputBridgeかで判定。
    /// EventSystem.currentがnullの場合はfalseを返す。
    /// </summary>
    /// <returns>ゲーム世界上ならtrue、UI上ならfalse。</returns>
    private bool IsPointerOverGameWorld()
    {
        if (EventSystem.current == null) return false;

        var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // 何かUIにヒットしていて、かつ最前面がInputBridgeならゲーム世界上と判断
        if (results.Count > 0 && results[0].gameObject.GetComponent<InputBridge>() != null)
        {
            return true;
        }

        // それ以外はUI上と判断
        return false;
    }

    /// <summary>
    /// マウス座標をゲームワールド座標に変換して返す。
    /// ScreenToWorldConverterのインスタンスが利用可能かつ変換成功すればその値を返す。
    /// 変換不可時は非常に遠い座標を返し、NPCが反応しないようにする。
    /// </summary>
    /// <returns>ワールド座標、もしくは変換不可時は大きな値の座標。</returns>
    private Vector3 GetMouseWorldPosition()
    {
        if (ScreenToWorldConverter.Instance != null &&
            ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
        {
            return worldPos;
        }

        return new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    }
}