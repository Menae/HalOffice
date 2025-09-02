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
    private enum PatrolSubState { Wandering, MovingToPOI, Inspecting }

    [Header("AIの状態")]
    [SerializeField] private NPCState currentState = NPCState.Patrol;

    [Header("移動と待機の設定")]
    public float moveSpeed = 1f;
    [Tooltip("NPCが歩く最短距離")]
    public float minWalkDistance = 2f;
    [Tooltip("NPCが歩く最長距離")]
    public float maxWalkDistance = 5f;
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
    [Tooltip("NPCが興味を示す家具などの場所リスト")]
    public List<Transform> pointsOfInterest;
    [Tooltip("家具などを調査する時間")]
    public float inspectDuration = 3.0f;

    [Header("警戒ステート設定 (Investigate)")]
    [Tooltip("視界にカーソルが入り続けたら警戒状態になるまでの時間")]
    public float sightAlertThreshold = 1.5f;
    [Tooltip("周囲を見渡して警戒する時間")]
    public float searchDuration = 4.0f;
    
    [Header("イベント発行")]
    [Tooltip("見つかり度を上げるためのイベントチャンネル")]
    public FloatEventChannelSO detectionIncreaseChannel;

    // 状態管理用の内部変数
    public bool isCursorInView { get; private set; }
    private bool isFacingRight = true;
    private Animator animator;
    private Rigidbody2D rb;

    private Coroutine behaviorCoroutine;
    private Vector3 investigationTarget; // 警戒時の目標地点
    private float timeInView = 0f; // 視界に入っている時間のカウンター
    private bool isMouseOverNPC = false; // カーソルがNPC本体の上にあるか

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

        // 開始時に最初の行動を開始する
        SwitchState(NPCState.Patrol);
    }

    void Update()
    {
        // 常に視界とカーソルの接触判定は行う
        CheckFieldOfView();
        HandleTriggers();
        
        if (fovObject != null)
        {
            fovObject.SetActive(isCursorInView);
        }

        // 現在のステートに応じたUpdate処理を呼び出す
        switch (currentState)
        {
            case NPCState.Patrol:
                UpdatePatrolState();
                break;
            case NPCState.Investigate:
                UpdateInvestigateState();
                break;
        }
    }

    void FixedUpdate()
    {
        // 物理的な移動はFixedUpdateで
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
        // 現在のステートの終了処理
        OnExitState();
        currentState = newState;
        // 新しいステートの開始処理
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
        while (currentState == NPCState.Patrol)
        {
            // 1. 目的なくうろつく (Wandering)
            if (Random.value > 0.5f) Flip();
            animator.SetBool("isWalking", true);
            float walkTimer = Random.Range(minWalkDistance, maxWalkDistance) / moveSpeed;
            yield return new WaitForSeconds(walkTimer);
            animator.SetBool("isWalking", false);
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

            if (pointsOfInterest == null || pointsOfInterest.Count == 0) continue;

            // 2. 家具に向かって歩く (MovingToPOI)
            Transform targetPOI = pointsOfInterest[Random.Range(0, pointsOfInterest.Count)];
            investigationTarget = targetPOI.position;

            // まず、移動を開始する前に「一度だけ」向きを決定する
            FaceTowards(investigationTarget);

            // 歩行を開始する
            animator.SetBool("isWalking", true);

            // X軸の距離で到着を判定する
            while (Mathf.Abs(transform.position.x - investigationTarget.x) > 0.5f)
            {
                if (currentState != NPCState.Patrol) yield break;
                yield return null;
            }
            animator.SetBool("isWalking", false);

            // 3. 家具を調べる (Inspecting)
            FaceTowards(investigationTarget); // 調査開始時に念のため再度向きを合わせる
            animator.SetBool("isInspecting", true);
            yield return new WaitForSeconds(inspectDuration);
            animator.SetBool("isInspecting", false);

            if (Random.value > 0.5f) Flip();
        }
    }

    private IEnumerator InvestigateRoutine()
    {
        // 1. 調査地点に向かう
        if (Vector2.Distance(transform.position, investigationTarget) > 0.5f)
        {
            animator.SetBool("isWalking", true);
            while (Vector2.Distance(transform.position, investigationTarget) > 0.5f)
            {
                FaceTowards(investigationTarget); // 移動中も向きを合わせる
                yield return null;
            }
            animator.SetBool("isWalking", false);
        }
        
        // 2. その場で周囲を見回す
        FaceTowards(investigationTarget);
        animator.SetBool("isSearching", true);
        yield return new WaitForSeconds(searchDuration);
        animator.SetBool("isSearching", false);

        // 3. 通常ステートに戻る
        SwitchState(NPCState.Patrol);
    }
    
    private void UpdatePatrolState()
    {
        // ▼▼▼ 修正点: 処理をコルーチンに移動したため、このメソッドは空にする ▼▼▼
    }

    private void UpdateInvestigateState()
    {
        // 警戒ステート中に毎フレーム実行したい処理があればここに記述
    }

    private void HandleTriggers()
    {
        if (currentState == NPCState.Investigate || !GameManager.Instance.isInputEnabled) return;

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
        if (EventSystem.current.IsPointerOverGameObject())
        {
            isCursorInView = false;
            return;
        }

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
        Vector3 cursorPos = Input.mousePosition;
        float distance_from_camera = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);
        cursorPos.z = distance_from_camera;
        return Camera.main.ScreenToWorldPoint(cursorPos);
    }
}