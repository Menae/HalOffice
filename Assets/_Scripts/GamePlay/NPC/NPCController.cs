using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NPCController : MonoBehaviour
{
    // Enumの定義を修正
    // "家具へ向かう"という新しい状態を追加
    public enum NPCState { Patrol, Investigate, HeadToFurniture }

    [Header("AIの状態")]
    [SerializeField] private NPCState currentState = NPCState.Patrol;

    [Header("移動と待機の設定")]
    public float moveSpeed = 1f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    [Header("視界設定")]
    public float viewRadius = 5f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public Vector2 eyeOffset = new Vector2(0.2f, 0.3f);

    [Header("関連オブジェクト")]
    public GameObject fovObject;

    [Header("通常ルーティーン設定 (Patrol)")]
    public List<Transform> pointsOfInterest;
    public float inspectDuration = 3.0f;
    [Range(0, 100)] // ← この行を追加
    [Tooltip("目的地到着後、フィジェットモーションを行う確率(%)")] // ← この行を追加
    public float fidgetProbability = 20f; // ← この行を追加

    [Header("家具への移動設定 (HeadToFurniture)")]
    [Tooltip("奥の家具の位置リスト")]
    public List<Transform> furnitureTargets;
    [Tooltip("家具の前で待機する時間")]
    public float furnitureWaitDuration = 5.0f;

    [Header("警戒ステート設定 (Investigate)")]
    public float sightAlertThreshold = 1.5f;
    public float searchDuration = 4.0f;
    public float detectionCheckRadius = 2.0f;

    [Header("イベント発行")]
    public FloatEventChannelSO detectionIncreaseChannel;


    // --- 内部変数 ---
    public bool isCursorInView { get; private set; }
    private bool isFacingRight = true;
    private Animator animator;
    private Rigidbody2D rb;
    private Coroutine behaviorCoroutine;
    private Vector3 investigationTarget;
    private float timeInView = 0f;
    private bool isMouseOverNPC = false;
    private Vector3 initialPosition; // NPCの初期Y座標を記憶
    private Transform lastVisitedPOI;


    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        initialPosition = transform.position; // 初期位置を記憶

        if (fovObject != null) fovObject.SetActive(false);
        OnEnterState();
    }

    void Update()
    {
        CheckFieldOfView();
        HandleTriggers();
        UpdateRigidbodyVelocity(); // Rigidbodyの速度更新をまとめる

        if (fovObject != null) fovObject.SetActive(isCursorInView);

        // デバッグ用：'F'キーで家具へ向かう挙動をテスト
        if (Input.GetKeyDown(KeyCode.F))
        {
            SwitchState(NPCState.HeadToFurniture);
        }
    }

    /// <summary>
    /// Animatorのパラメータに応じてRigidbodyの速度を更新する
    /// </summary>
    private void UpdateRigidbodyVelocity()
    {
        float moveX = animator.GetFloat("moveX");
        float moveY = animator.GetFloat("moveY");
        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;

        rb.velocity = moveDirection * moveSpeed;

        // moveXがプラス（右に進みたい）のに、左を向いていたら
        if (moveX > 0.01f && isFacingRight == false)
        {
            Flip(); // 右を向かせる
        }
        // moveXがマイナス（左に進みたい）のに、右を向いていたら
        else if (moveX < -0.01f && isFacingRight == true)
        {
            Flip(); // 左を向かせる
        }
    }

    private void SwitchState(NPCState newState)
    {
        if (currentState == newState) return;

        OnExitState();
        currentState = newState;
        OnEnterState();
    }

    private void OnEnterState()
    {
        if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);

        switch (currentState)
        {
            case NPCState.Patrol:
                behaviorCoroutine = StartCoroutine(PatrolRoutine());
                break;
            case NPCState.Investigate:
                behaviorCoroutine = StartCoroutine(InvestigateRoutine());
                break;
            case NPCState.HeadToFurniture:
                behaviorCoroutine = StartCoroutine(HeadToFurnitureRoutine());
                break;
        }
    }

    private void OnExitState()
    {
        if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);
        // 全ての移動パラメータを0にリセット
        animator.SetFloat("moveX", 0f);
        animator.SetFloat("moveY", 0f);

        timeInView = 0f;
    }

    private IEnumerator PatrolRoutine()
    {
        if (pointsOfInterest == null || pointsOfInterest.Count < 2)
        {
            Debug.LogWarning("NPCController: PointsOfInterestが2つ以上設定されていません。パトロールを停止します。", this.gameObject);
            yield break;
        }

        while (currentState == NPCState.Patrol)
        {
            // 新しい目的地選定ロジック
            Transform targetPOI;
            do
            {
                targetPOI = pointsOfInterest[Random.Range(0, pointsOfInterest.Count)];
            }
            while (targetPOI == lastVisitedPOI); // 前回と同じ場所なら、選び直す

            lastVisitedPOI = targetPOI; // 今回の目的地を記憶する

            Vector3 targetPosition = new Vector3(targetPOI.position.x, initialPosition.y, targetPOI.position.z);

            // 目的地に向かって歩き始める
            animator.SetFloat("moveX", Mathf.Sign(targetPosition.x - transform.position.x));

            // 目的地に到着するまで、このループを繰り返す
            while (Mathf.Abs(transform.position.x - targetPosition.x) > 0.5f)
            {
                yield return null;
            }

            // 到着したので停止する
            animator.SetFloat("moveX", 0f);

            // 0から100までの乱数を1回だけ生成し、設定した確率より小さいかチェック
            if (Random.Range(0f, 100f) < fidgetProbability)
            {
                animator.SetTrigger("DoFidget");
            }

            // 目的地で待機（ここからはフィジェット判定を削除）
            yield return new WaitForSeconds(inspectDuration);

            // 次の目的地に向かう前に少し待つ
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        }
    }

    private IEnumerator InvestigateRoutine()
    {
        // 1. 警戒モーションを開始
        animator.SetTrigger("isLooking");

        // 2. 警戒アニメーションの長さに合わせて待機（searchDurationで調整）
        yield return new WaitForSeconds(searchDuration);

        // 3. 警戒中にカーソルが近ければ見つかり度を上昇（既存機能）
        float distanceToCursor = Vector3.Distance(transform.position, GetMouseWorldPosition());
        if (distanceToCursor < detectionCheckRadius)
        {
            Debug.Log("警戒中にカーソルが近すぎたため、見つかり度が上昇！");
            if (detectionIncreaseChannel != null) detectionIncreaseChannel.RaiseEvent(40f);
        }

        // 4. 通常ステートに戻る
        SwitchState(NPCState.Patrol);
    }

    private IEnumerator HeadToFurnitureRoutine()
    {
        // ★リストが空かどうかのチェックに変更
        if (furnitureTargets == null || furnitureTargets.Count == 0)
        {
            Debug.LogWarning("家具の目標地点リスト(furnitureTargets)が設定されていません。", this.gameObject);
            SwitchState(NPCState.Patrol);
            yield break;
        }

        // ★リストからランダムに目的地を一つ選ぶ
        Transform targetFurniture = furnitureTargets[Random.Range(0, furnitureTargets.Count)];

        // 1. 奥向きに歩く
        animator.SetFloat("moveY", 1f);
        // ★選んだ目的地に向かうように修正
        yield return new WaitUntil(() => Vector3.Distance(transform.position, targetFurniture.position) < 0.5f);
        animator.SetFloat("moveY", 0f);

        // 2. 家具の前で待機
        yield return new WaitForSeconds(furnitureWaitDuration);

        // 3. 手前に歩いて元のY座標に戻る
        animator.SetFloat("moveY", -1f);
        yield return new WaitUntil(() => Mathf.Abs(transform.position.y - initialPosition.y) < 0.1f);
        animator.SetFloat("moveY", 0f);

        // 4. パトロールに戻る
        SwitchState(NPCState.Patrol);
    }

    /// <summary>
    /// 外部のオブジェクト（家具など）から、家具へ向かう行動をリクエストされるメソッド
    /// </summary>
    public void RequestFurnitureInteraction()
    {
        // パトロール中である場合のみ、状態を切り替える
        if (currentState == NPCState.Patrol)
        {
            SwitchState(NPCState.HeadToFurniture);
        }
    }

    private void HandleTriggers()
    {
        if (currentState == NPCState.Investigate || currentState == NPCState.HeadToFurniture || (GameManager.Instance != null && !GameManager.Instance.isInputEnabled)) return;

        if (isCursorInView)
        {
            timeInView += Time.deltaTime;
            if (timeInView > sightAlertThreshold)
            {
                Debug.Log("視界に入りすぎたため警戒！");
                investigationTarget = GetMouseWorldPosition();
                SwitchState(NPCState.Investigate);
                return;
            }
        }
        else
        {
            timeInView = 0f;
        }

        if (isMouseOverNPC)
        {
            Debug.Log("体に触れられたため警戒！");
            investigationTarget = transform.position;
            SwitchState(NPCState.Investigate);

            isMouseOverNPC = false;
        }
    }

    public void HearSound(Vector3 soundPosition, float detectionAmount)
    {
        if (currentState == NPCState.Investigate) return;

        if (detectionIncreaseChannel != null) detectionIncreaseChannel.RaiseEvent(detectionAmount);
        investigationTarget = soundPosition;
        SwitchState(NPCState.Investigate);
    }

    private void CheckFieldOfView()
    {
        // 1. マウスカーソルの下にUIがあるかを詳細にチェックする
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 2. ヒットしたUIの中に、ゲーム画面(Raw Image)以外のものが含まれているかチェック
        bool isOverRealUI = false;
        if (ScreenToWorldConverter.Instance != null && ScreenToWorldConverter.Instance.gameScreen != null)
        {
            foreach (var result in results)
            {
                // もしヒットしたのがゲーム画面でなければ、それは「本物のUI」の上だと判断
                if (result.gameObject != ScreenToWorldConverter.Instance.gameScreen.gameObject)
                {
                    isOverRealUI = true;
                    break; // 1つでも見つかればチェック終了
                }
            }
        }
        else if (results.Count > 0)
        {
            // ScreenToWorldConverterが設定されていない場合は、以前の挙動に fallback
            isOverRealUI = true;
        }

        // 3. もし「本物のUI」の上なら、視界検知をせずに終了
        if (isOverRealUI)
        {
            isCursorInView = false;
            return;
        }
        
        // --- 以下は既存の視界検知ロジック（変更なし） ---
        Vector3 eyePosition = transform.TransformPoint(eyeOffset);
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        float distanceToCursor = Vector3.Distance(eyePosition, mouseWorldPos);
        Vector3 directionToCursor = (mouseWorldPos - eyePosition).normalized;

        if (distanceToCursor < viewRadius && Vector3.Angle(GetDirection(), directionToCursor) < viewAngle / 2)
        {
            isCursorInView = true;
        }
        else
        {
            isCursorInView = false;
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * (isFacingRight ? 1 : -1), transform.localScale.y, transform.localScale.z);
    }

    private void FaceTowards(Vector3 targetPosition)
    {
        if ((targetPosition.x > transform.position.x && !isFacingRight) ||
            (targetPosition.x < transform.position.x && isFacingRight))
        {
            Flip();
        }
    }

    private void OnMouseEnter()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            isMouseOverNPC = true;
        }
    }

    private void OnMouseExit()
    {
        isMouseOverNPC = false;
    }

    public Vector3 GetDirection()
    {
        return isFacingRight ? transform.right : -transform.right;
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