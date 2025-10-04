using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NPCController : MonoBehaviour
{
    // ステートを定義するEnum
    public enum NPCState { Patrol, Investigate }

    [Header("AIの状態")]
    [SerializeField] private NPCState currentState = NPCState.Patrol;

    [Header("移動と待機の設定")]
    public float moveSpeed = 1f;
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;

    [Header("視界設定")]
    [Tooltip("この値が検知と描画の両方に使われます")]
    public float viewRadius = 5f;
    [Range(0, 360)]
    [Tooltip("この値が検知と描画の両方に使われます")]
    public float viewAngle = 90f;
    [Tooltip("視界の中心となる「目」の、オブジェクト中心からのズレ")]
    public Vector2 eyeOffset = new Vector2(0.2f, 0.3f);

    [Header("関連オブジェクト")]
    [Tooltip("視界範囲を表示するための子オブジェクト")]
    public GameObject fovObject;

    [Header("通常ルーティーン設定 (Patrol)")]
    [Tooltip("NPCが巡回する地点のリスト")]
    public List<Transform> pointsOfInterest;
    [Tooltip("各地点に到着してから次の目的地に向かうまでの待機時間")]
    public float inspectDuration = 3.0f;

    [Header("警戒ステート設定 (Investigate)")]
    [Tooltip("視界にカーソルが入り続けたら警戒状態になるまでの時間")]
    public float sightAlertThreshold = 1.5f;
    [Tooltip("周囲を見渡して警戒する時間")]
    public float searchDuration = 4.0f;
    [Tooltip("警戒ステート移行後、この半径以内にカーソルがあれば見つかり度が上昇する")]
    public float detectionCheckRadius = 2.0f;

    [Header("イベント発行")]
    [Tooltip("見つかり度を上げるためのイベントチャンネル")]
    public FloatEventChannelSO detectionIncreaseChannel;

    // 状態管理用の内部変数
    public bool isCursorInView { get; private set; }
    private bool isFacingRight = true;
    private Animator animator;
    private Rigidbody2D rb;

    private Coroutine behaviorCoroutine;
    private Vector3 investigationTarget;
    private float timeInView = 0f;
    private bool isMouseOverNPC = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        if (fovObject != null)
        {
            fovObject.SetActive(false);
        }

        SwitchState(NPCState.Patrol);
    }

    void Update()
    {
        CheckFieldOfView();
        HandleTriggers();

        if (fovObject != null)
        {
            fovObject.SetActive(isCursorInView);
        }

        switch (currentState)
        {
            case NPCState.Patrol:
                break;
            case NPCState.Investigate:
                UpdateInvestigateState();
                break;
        }
    }

    void FixedUpdate()
    {
        if (animator.GetBool("isWalking"))
        {
            rb.velocity = GetDirection() * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void SwitchState(NPCState newState)
    {
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
        }
    }

    private void OnExitState()
    {
        if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);
        animator.SetBool("isWalking", false);
        animator.SetBool("isSearching", false);
        animator.SetBool("isInspecting", false);
        timeInView = 0f;
    }

    private IEnumerator PatrolRoutine()
    {
        // pointsOfInterestが設定されていない場合は、エラーを防ぐために何もしない
        if (pointsOfInterest == null || pointsOfInterest.Count == 0)
        {
            Debug.LogWarning("NPCController: PointsOfInterestが設定されていないため、Patrol行動を停止します。", this.gameObject);
            yield break; // コルーチンを終了
        }

        // currentStateがPatrolである限り、永遠に巡回を続ける
        while (currentState == NPCState.Patrol)
        {
            // 1. 目的地をランダムに選ぶ
            Transform targetPOI = pointsOfInterest[Random.Range(0, pointsOfInterest.Count)];
            Vector3 targetPosition = targetPOI.position;

            // 2. 目的地に向かって歩く
            FaceTowards(targetPosition);
            animator.SetBool("isWalking", true);
            while (Mathf.Abs(transform.position.x - targetPosition.x) > 0.5f)
            {
                if (currentState != NPCState.Patrol) yield break; // 途中で警戒ステートに切り替わったら中断
                yield return null;
            }
            animator.SetBool("isWalking", false);

            // 3. 目的地で待機＆調査
            FaceTowards(targetPosition);
            animator.SetBool("isInspecting", true);
            yield return new WaitForSeconds(inspectDuration);
            animator.SetBool("isInspecting", false);

            // 4. 次の目的地に向かう前に少し待つ
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        }
    }

    private IEnumerator InvestigateRoutine()
    {
        // ▼▼▼ ここからが追加ロジック ▼▼▼
        // 1. 警戒ステートに移行して0.5秒待機
        yield return new WaitForSeconds(0.5f);

        // 2. カーソルとの距離を測定
        float distanceToCursor = Vector3.Distance(transform.position, GetMouseWorldPosition());

        // 3. もし距離が指定した半径以内なら、見つかり度を40上昇させる
        if (distanceToCursor < detectionCheckRadius)
        {
            Debug.Log("警戒中にカーソルが近すぎたため、見つかり度が上昇！");
            if (detectionIncreaseChannel != null)
            {
                detectionIncreaseChannel.RaiseEvent(40f);
            }
        }
        // ▲▲▲ ここまで ▲▲▲

        // --- 以下は既存のロジック ---
        // 4. 調査地点に向かう
        if (Vector3.Distance(transform.position, investigationTarget) > 0.5f)
        {
            animator.SetBool("isWalking", true);
            while (Mathf.Abs(transform.position.x - investigationTarget.x) > 0.1f)
            {
                FaceTowards(investigationTarget);
                yield return null;
            }
            animator.SetBool("isWalking", false);
        }
        
        // 5. その場で周囲を見回す
        FaceTowards(investigationTarget);
        animator.SetBool("isSearching", true);
        yield return new WaitForSeconds(searchDuration);
        animator.SetBool("isSearching", false);

        // 6. 通常ステートに戻る
        SwitchState(NPCState.Patrol);
    }

    private void UpdateInvestigateState()
    {
        // 警戒ステート中に毎フレーム実行したい処理があればここに記述
    }

    private void HandleTriggers()
    {
        if (currentState == NPCState.Investigate || (GameManager.Instance != null && !GameManager.Instance.isInputEnabled)) return;

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
        }
    }

    public void HearSound(Vector3 soundPosition, float detectionAmount)
    {
        if (currentState == NPCState.Investigate) return; 
        
        Debug.Log($"音源({soundPosition})を検知！警戒します。");

        if (detectionIncreaseChannel != null)
        {
            detectionIncreaseChannel.RaiseEvent(detectionAmount);
        }

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