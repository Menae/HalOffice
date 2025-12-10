using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
/// <summary>
/// NPCの振る舞い、視界判定、そして家具やアイテムへの反応を管理するコンポーネント。
/// Rigidbody2DとCollider2Dを必須とし、Startで初期化、Updateで視界と状態遷移を評価する。
/// </summary>
public class NPCController : MonoBehaviour
{
    /// <summary>NPCの行動状態を表す。パトロール/警戒/家具へ向かうの3種。</summary>
    public enum NPCState { Patrol, Investigate, HeadToFurniture }

    [Header("AIの状態")]
    [SerializeField]
    /// <summary>現在の状態。Inspectorで初期状態を設定可能。</summary>
    private NPCState currentState = NPCState.Patrol;

    [Header("移動と待機の設定")]
    /// <summary>移動速度。Animatorの入力値に乗算して使用。</summary>
    public float moveSpeed = 1f;
    /// <summary>待機時間の下限（秒）。ランダム待機で使用。</summary>
    public float minWaitTime = 2f;
    /// <summary>待機時間の上限（秒）。ランダム待機で使用。</summary>
    public float maxWaitTime = 5f;

    [Header("視界設定")]
    /// <summary>視認可能な最大距離（ワールド単位）。</summary>
    public float viewRadius = 5f;
    [Range(0, 360)]
    /// <summary>視野角（度）。中央からの半角で判定。</summary>
    public float viewAngle = 90f;
    /// <summary>目の位置オフセット（ローカル座標）。視界判定の起点として使用。</summary>
    public Vector2 eyeOffset = new Vector2(0.2f, 0.3f);

    [Header("関連オブジェクト")]
    /// <summary>視界表示用オブジェクト。視界内なら有効化することがある。</summary>
    public GameObject fovObject;
    /// <summary>シーン内のスロット管理コンポーネント（InspectorでD&D）。nullチェックあり。</summary>
    public ObjectSlotManager objectSlotManager;
    /// <summary>吹き出しのプレハブ（WorldSpace用）。null時は表示処理をスキップ。</summary>
    public GameObject speechBubblePrefab;
    /// <summary>吹き出し生成位置の基準Transform。未設定時はNPC頭上に生成。</summary>
    public Transform bubbleAnchor;
    /// <summary>視覚表現のTransform（左右反転で使用）。InspectorでD&D。</summary>
    public Transform visualsTransform;
    /// <summary>Animator参照（移動/トリガー制御）。InspectorでD&D。</summary>
    public Animator animator;
    [Tooltip("ゲームの進行状態（チュートリアル中か）を監視するために使用")]
    /// <summary>Day1の進行を監視するManager。未設定ならシーンから検索を試みる。</summary>
    public Day1Manager day1Manager;

    [Header("通常ルーティーン設定 (Patrol)")]
    /// <summary>巡回地点のリスト。InspectorでTransformを割り当てること。</summary>
    public List<Transform> pointsOfInterest;
    /// <summary>目的地での調査時間（秒）。到着後に待機する時間。</summary>
    public float inspectDuration = 3.0f;
    [Range(0, 100)]
    [Tooltip("目的地到着後、フィジェットモーションを行う確率(%)")]
    /// <summary>到着時にフィジェットアニメを再生する確率（0-100）。</summary>
    public float fidgetProbability = 20f;

    [Header("家具への移動設定 (HeadToFurniture)")]
    [Tooltip("奥の家具の位置リスト")]
    /// <summary>家具へ向かう際に選択する候補Transformのリスト。空なら遷移をキャンセル。</summary>
    public List<Transform> furnitureTargets;
    [Tooltip("家具の前で待機する時間")]
    /// <summary>家具到達後に待機する秒数（ランダム待機の代替ではなく固定）。</summary>
    public float furnitureWaitDuration = 5.0f;

    [Header("警戒ステート設定 (Investigate)")]
    /// <summary>視界に入ってから警戒状態に遷移するまでの秒数。</summary>
    public float sightAlertThreshold = 1.5f;
    /// <summary>調査アニメーションや行動を行うための待機時間（秒）。</summary>
    public float searchDuration = 4.0f;
    /// <summary>近接判定で使用する距離（ワールド単位）。</summary>
    public float detectionCheckRadius = 2.0f;

    [Header("イベント発行")]
    /// <summary>検出値の増加を通知するイベントチャンネル（Inspectorで割り当て）。nullチェックあり。</summary>
    public FloatEventChannelSO detectionIncreaseChannel;


    // --- 内部変数 ---
    /// <summary>カーソルが視界内にあるかのフラグ。外部読み取り専用。</summary>
    public bool isCursorInView { get; private set; }
    /// <summary>現在の向き。右を向いていればtrue。</summary>
    private bool isFacingRight = true;
    private Rigidbody2D rb;
    private Coroutine behaviorCoroutine;
    private Vector3 investigationTarget;
    private float timeInView = 0f;
    private bool isMouseOverNPC = false;
    /// <summary>NPCの起点位置（Startで取得）。Y座標の復帰や移動基準に使用。</summary>
    private Vector3 initialPosition;
    private Transform lastVisitedPOI;


    /// <summary>
    /// Unity Start。オブジェクト生成直後に1度だけ呼ばれる。
    /// 初期Transform/Rigidbody/Animatorの設定とday1Managerのフォールバック検索を行う。
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        initialPosition = transform.position;

        // day1Managerがインスペクタで設定されていなければ、シーンから探す
        if (day1Manager == null)
        {
            day1Manager = FindObjectOfType<Day1Manager>();
            if (day1Manager == null)
            {
                Debug.LogWarning("NPCControllerがDay1Managerを見つけられません。チュートリアル中の停止が機能しない可能性があります。", this);
            }
        }

        if (fovObject != null) fovObject.SetActive(false);
        OnEnterState();
    }

    /// <summary>
    /// Unity Update。毎フレーム呼ばれ、ゲーム進行状態や視界・トリガー処理を評価する。
    /// チュートリアル中は動作を停止し、終了直後に再開処理を行う。
    /// </summary>
    void Update()
    {
        // 1. チュートリアル中かどうかの判定
        if (day1Manager != null && !day1Manager.isGameActive)
        {
            // チュートリアル中はコルーチンを停止して完全に停止させる
            if (behaviorCoroutine != null)
            {
                StopCoroutine(behaviorCoroutine);
                behaviorCoroutine = null;
            }

            animator.SetFloat("moveX", 0f);
            animator.SetFloat("moveY", 0f);
            rb.velocity = Vector2.zero;

            // このフレームの以降の処理を全てスキップ
            return;
        }

        // チュートリアルが終了した瞬間の再開処理（ゲーム開始直後にコルーチンを再起動）
        if (day1Manager != null && day1Manager.isGameActive && behaviorCoroutine == null)
        {
            OnEnterState();
        }

        CheckFieldOfView();
        HandleTriggers();
        UpdateRigidbodyVelocity();

        if (fovObject != null) fovObject.SetActive(isCursorInView);

        // デバッグ用：'F'キーで家具へ向かう挙動をテスト
        if (Input.GetKeyDown(KeyCode.F))
        {
            SwitchState(NPCState.HeadToFurniture);
        }
    }

    /// <summary>
    /// Animatorのパラメータに応じてRigidbodyの速度を設定する。
    /// Animatorからの入力を正規化して移動速度に乗算することで滑らかな移動を実現。
    /// </summary>
    private void UpdateRigidbodyVelocity()
    {
        float moveX = animator.GetFloat("moveX");
        float moveY = animator.GetFloat("moveY");
        Vector2 moveDirection = new Vector2(moveX, moveY).normalized;

        rb.velocity = moveDirection * moveSpeed;

        // 向き反転処理（X方向の入力に基づく）
        if (moveX > 0.01f && isFacingRight == false)
        {
            Flip();
        }
        else if (moveX < -0.01f && isFacingRight == true)
        {
            Flip();
        }
    }

    /// <summary>
    /// 状態を切り替える。新旧が同じ場合は何もしない。
    /// </summary>
    /// <param name="newState">遷移先の状態。</param>
    private void SwitchState(NPCState newState)
    {
        if (currentState == newState) return;

        OnExitState();
        currentState = newState;
        OnEnterState();
    }

    /// <summary>
    /// 現在の状態に応じたコルーチンを開始する。
    /// 既存の動作コルーチンがあれば停止してから開始する。
    /// </summary>
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

    /// <summary>
    /// 状態終了時に呼ばれる。動作コルーチン停止とアニメータ入力のリセットを行う。
    /// seen時間等の内部カウンタも初期化する。
    /// </summary>
    private void OnExitState()
    {
        if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);
        animator.SetFloat("moveX", 0f);
        animator.SetFloat("moveY", 0f);

        timeInView = 0f;
    }

    /// <summary>
    /// パトロールルーチン。ポイント間を移動し、到着時にスロットの状態を確認して反応する。
    /// pointsOfInterestが未設定または2未満の場合は中断する。
    /// </summary>
    private IEnumerator PatrolRoutine()
    {
        if (pointsOfInterest == null || pointsOfInterest.Count < 2)
        {
            Debug.LogWarning("NPCController: PointsOfInterestが2つ以上設定されていません。パトロールを停止します。", this.gameObject);
            yield break;
        }

        while (currentState == NPCState.Patrol)
        {
            Transform targetPOI;
            do
            {
                targetPOI = pointsOfInterest[Random.Range(0, pointsOfInterest.Count)];
            }
            while (targetPOI == lastVisitedPOI);

            lastVisitedPOI = targetPOI;

            Vector3 targetPosition = new Vector3(targetPOI.position.x, initialPosition.y, targetPOI.position.z);

            // X方向の入力をAnimatorに与えることで歩行アニメーションを誘発
            animator.SetFloat("moveX", Mathf.Sign(targetPosition.x - transform.position.x));

            while (Mathf.Abs(transform.position.x - targetPosition.x) > 0.5f)
            {
                yield return null;
            }

            animator.SetFloat("moveX", 0f);

            // 到着した目的地に紐づくスロットを取得（null安全）
            ObjectSlot currentSlot = objectSlotManager.objectSlots.Find(s => s.slotTransform == lastVisitedPOI);
            if (currentSlot != null)
            {
                // アイテム消失チェック（初期配置にあり、現在空なら反応）
                if (objectSlotManager.InitialSlotContents.ContainsKey(currentSlot) && !currentSlot.IsOccupied())
                {
                    yield return StartCoroutine(ReactToMissingObjectRoutine(currentSlot));
                    objectSlotManager.InitialSlotContents.Remove(currentSlot);
                }
                // 新規配置アイテム発見チェック
                else if (objectSlotManager.IsNewlyPlaced(currentSlot))
                {
                    yield return StartCoroutine(ReactToNewObjectRoutine(currentSlot));
                    objectSlotManager.MarkSlotAsSeen(currentSlot);
                }
            }

            // フィジェット判定（確率）
            if (Random.Range(0f, 100f) < fidgetProbability)
            {
                animator.SetTrigger("DoFidget");
            }

            // 調査時間の待機
            yield return new WaitForSeconds(inspectDuration);

            // 次の目的地前のランダム待機
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        }
    }

    /// <summary>
    /// 調査（Investigate）ルーチン。警戒アニメを行い、一定時間後にパトロールへ戻す。
    /// カーソルや音源による詳細な検査は別処理でイベント発行を行う。
    /// </summary>
    private IEnumerator InvestigateRoutine()
    {
        animator.SetTrigger("isLooking");
        yield return new WaitForSeconds(searchDuration);

        SwitchState(NPCState.Patrol);
    }

    /// <summary>
    /// 家具へ向かうルーチン。最も近い家具を選び、到達→待機→初期位置へ復帰する。
    /// furnitureTargetsが未設定または空ならパトロールに戻す。
    /// </summary>
    private IEnumerator HeadToFurnitureRoutine()
    {
        if (furnitureTargets == null || furnitureTargets.Count == 0)
        {
            Debug.LogWarning("家具の目標地点リスト(furnitureTargets)が設定されていません。", this.gameObject);
            SwitchState(NPCState.Patrol);
            yield break;
        }

        Transform nearestFurniture = null;
        float nearestDistance = float.MaxValue;

        foreach (Transform furniture in furnitureTargets)
        {
            float distance = Vector3.Distance(transform.position, furniture.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestFurniture = furniture;
            }
        }

        if (nearestFurniture == null)
        {
            SwitchState(NPCState.Patrol);
            yield break;
        }

        // Y方向移動の入力をAnimatorに与えて移動を誘導
        animator.SetFloat("moveY", 1f);
        yield return new WaitUntil(() => Vector3.Distance(transform.position, nearestFurniture.position) < 0.5f);
        animator.SetFloat("moveY", 0f);

        // 到着後ランダム待機
        float randomWait = Random.Range(2f, 6f);
        yield return new WaitForSeconds(randomWait);

        // 初期位置へ戻る
        animator.SetFloat("moveY", -1f);
        yield return new WaitUntil(() => Mathf.Abs(transform.position.y - initialPosition.y) < 0.1f);
        animator.SetFloat("moveY", 0f);

        SwitchState(NPCState.Patrol);
    }

    /// <summary>
    /// 新規に配置されたオブジェクトを発見した際の反応ルーチン。
    /// Inkファイルと吹き出しプレハブが設定されている場合のみセリフを表示する。
    /// </summary>
    private IEnumerator ReactToNewObjectRoutine(ObjectSlot noticedSlot)
    {
        Draggable currentObject = noticedSlot.currentObject;
        if (currentObject == null || currentObject.itemData == null)
        {
            yield break;
        }

        ItemData newItemData = currentObject.itemData;
        float waitDuration = 3.0f;

        if (newItemData.placedReactionInk != null && speechBubblePrefab != null)
        {
            var story = new Story(newItemData.placedReactionInk.text);
            string reactionText = "！";

            if (story.canContinue)
            {
                reactionText = story.Continue().Trim();
            }

            Vector3 spawnPos = bubbleAnchor != null ? bubbleAnchor.position : transform.position + Vector3.up * 1.5f;
            GameObject bubbleInstance = Instantiate(speechBubblePrefab, spawnPos, Quaternion.identity, transform);
            var bubbleController = bubbleInstance.GetComponent<WorldSpaceBubbleController>();

            if (bubbleController != null)
            {
                bubbleController.ShowMessage(reactionText, waitDuration);
            }
        }
        else
        {
            Debug.LogWarning($"No 'placedReactionInk' or bubble prefab set for item: '{newItemData.itemName}'");
        }

        yield return new WaitForSeconds(waitDuration);
    }

    /// <summary>
    /// 初期配置されていたアイテムが消失した場合の反応ルーチン。
    /// 初期マップの内容を参照してセリフを表示する。必要リソースが無ければログを出す。
    /// </summary>
    private IEnumerator ReactToMissingObjectRoutine(ObjectSlot noticedSlot)
    {
        ItemData missingItemData = objectSlotManager.InitialSlotContents[noticedSlot];
        float waitDuration = 3.0f;

        if (missingItemData != null && missingItemData.missingReactionInk != null && speechBubblePrefab != null)
        {
            var story = new Story(missingItemData.missingReactionInk.text);
            string reactionText = "……？";

            if (story.canContinue)
            {
                reactionText = story.Continue().Trim();
            }

            Vector3 spawnPos = bubbleAnchor != null ? bubbleAnchor.position : transform.position + Vector3.up * 1.5f;
            GameObject bubbleInstance = Instantiate(speechBubblePrefab, spawnPos, Quaternion.identity, transform);
            var bubbleController = bubbleInstance.GetComponent<WorldSpaceBubbleController>();

            if (bubbleController != null)
            {
                bubbleController.ShowMessage(reactionText, waitDuration);
            }
        }
        else
        {
            Debug.LogWarning($"Missing reaction ink or bubble prefab for item: '{missingItemData?.itemName}'");
        }

        yield return new WaitForSeconds(waitDuration);
    }

    /// <summary>
    /// 外部から家具への反応を要求するための公開メソッド。
    /// パトロール中のみ状態をHeadToFurnitureに切り替える。
    /// </summary>
    public void RequestFurnitureInteraction()
    {
        if (currentState == NPCState.Patrol)
        {
            SwitchState(NPCState.HeadToFurniture);
        }
    }

    /// <summary>
    /// トリガー判定の集約処理。視界とマウス接触に基づく警戒遷移を行う。
    /// InvestigateやHeadToFurniture中、または入力が無効な場合は早期リターンする。
    /// </summary>
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

    /// <summary>
    /// 外部から音を通知し、警戒度を増加させつつInvestigate状態に遷移させる。
    /// detectionIncreaseChannelが設定されていれば検出増加値を発行する。
    /// </summary>
    /// <param name="soundPosition">音源のワールド座標。</param>
    /// <param name="detectionAmount">増加させる検出値。</param>
    public void HearSound(Vector3 soundPosition, float detectionAmount)
    {
        if (currentState == NPCState.Investigate) return;

        if (detectionIncreaseChannel != null) detectionIncreaseChannel.RaiseEvent(detectionAmount);
        investigationTarget = soundPosition;
        SwitchState(NPCState.Investigate);
    }

    /// <summary>
    /// マウスおよびUIを考慮した視界チェックを実行する。
    /// UI上であれば視界判定をスキップし、ScreenToWorldConverterによる座標変換を優先利用する。
    /// </summary>
    private void CheckFieldOfView()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool isOverRealUI = false;
        if (ScreenToWorldConverter.Instance != null && ScreenToWorldConverter.Instance.gameScreen != null)
        {
            foreach (var result in results)
            {
                if (result.gameObject != ScreenToWorldConverter.Instance.gameScreen.gameObject)
                {
                    isOverRealUI = true;
                    break;
                }
            }
        }
        else if (results.Count > 0)
        {
            isOverRealUI = true;
        }

        if (isOverRealUI)
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

    /// <summary>
    /// 視覚表現を左右反転する。visualsTransformのlocalScaleを変更して反転を行う。
    /// </summary>
    private void Flip()
    {
        isFacingRight = !isFacingRight;

        visualsTransform.localScale = new Vector3(
            Mathf.Abs(visualsTransform.localScale.x) * (isFacingRight ? 1 : -1),
            visualsTransform.localScale.y,
            visualsTransform.localScale.z
        );
    }

    /// <summary>
    /// 指定位置に向けて向きを合わせる（X座標差に基づく）。必要ならFlipを呼ぶ。
    /// </summary>
    /// <param name="targetPosition">注視すべきワールド座標。</param>
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

    /// <summary>
    /// 現在の向きを基に正面方向の単位ベクトルを返す。右向きならtransform.right、左向きならその逆。
    /// </summary>
    /// <returns>NPCの向いている方向のベクトル。</returns>
    public Vector3 GetDirection()
    {
        return isFacingRight ? transform.right : -transform.right;
    }

    /// <summary>
    /// マウス座標をゲームワールド座標に変換して返す。
    /// ScreenToWorldConverterが利用可能で変換可能な場合はその値を返し、そうでなければ極端に遠い座標を返すことで反応を防ぐ。
    /// </summary>
    /// <returns>ワールド座標、あるいは変換不可時は非常に大きな値の座標。</returns>
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