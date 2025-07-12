using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class NPCController : MonoBehaviour
{
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

    public bool isCursorInView { get; private set; }
    private bool isFacingRight = true;
    private Animator animator;
    private Rigidbody2D rb;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (fovObject != null)
        {
            fovObject.SetActive(false);
        }
        StartCoroutine(NPCBehaviorRoutine());
    }

    void FixedUpdate()
    {
        // アニメーターが歩行状態の時だけ、物理的な力を加える
        if (animator.GetBool("isWalking"))
        {
            rb.velocity = GetDirection() * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero; // 止まっている時は速度をゼロに
        }
    }

    void Update()
    {
        CheckFieldOfView();
        if (fovObject != null)
        {
            fovObject.SetActive(isCursorInView);
        }
    }

    private IEnumerator NPCBehaviorRoutine()
    {
        while (true)
        {
            //待機フェーズ
            animator.SetBool("isWalking", false);
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

            //歩行準備
            Flip();

            //歩行フェーズ
            animator.SetBool("isWalking", true);
            //ランダムな「時間」だけ歩行状態を維持する
            float walkTimer = Random.Range(minWalkDistance, maxWalkDistance) / moveSpeed; //距離を速度で割って時間を計算
            yield return new WaitForSeconds(walkTimer);
        }
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
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
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