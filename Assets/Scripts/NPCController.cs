using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

// Animatorコンポーネントが必須であることを示す
[RequireComponent(typeof(Animator))]
public class NPCController : MonoBehaviour
{
    [Header("移動と待機の設定")]
    public float moveSpeed = 1f;
    public float walkTime = 1.5f;
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

    // --- 内部で使う変数 ---
    public bool isCursorInView { get; private set; }
    private bool isFacingRight = true;
    private Animator animator; // アニメーションを制御するための変数

    void Start()
    {
        // Animatorコンポーネントを取得
        animator = GetComponent<Animator>();

        // 開始時は必ず視界を非表示にする
        if (fovObject != null)
        {
            fovObject.SetActive(false);
        }

        // NPCの行動パターンを開始
        StartCoroutine(NPCBehaviorRoutine());
    }

    void Update()
    {
        CheckFieldOfView();

        // isCursorInViewの値に応じて、視界オブジェクトの表示/非表示を切り替える
        if (fovObject != null)
        {
            fovObject.SetActive(isCursorInView);
        }
    }

    private IEnumerator NPCBehaviorRoutine()
    {
        // このループを無限に繰り返す
        while (true)
        {
            // --- 待機フェーズ ---
            // アニメーションを「待機」状態にする
            animator.SetBool("isWalking", false);

            // ランダムな時間だけ待機
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

            // --- 歩行準備 ---
            // キャラクターを反転させる
            Flip();

            // --- 歩行フェーズ ---
            // アニメーションを「歩行」状態にする
            animator.SetBool("isWalking", true);

            // 指定された時間だけ歩き続ける
            float walkTimer = 0f;
            while (walkTimer < walkTime)
            {
                // 正しい向きに、ワールド座標で移動
                transform.position += GetDirection() * moveSpeed * Time.deltaTime;
                walkTimer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void CheckFieldOfView()
    {
        // もしマウスポインターがUI要素の上にあれば、NPCは検知しない
        if (EventSystem.current.IsPointerOverGameObject())
        {
            isCursorInView = false;
            return; // この場で処理を終了し、以降の距離や角度の計算は行わない
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
        // 論理的な向きを反転
        isFacingRight = !isFacingRight;
        // 見た目の向きを反転
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }

    public Vector3 GetDirection()
    {
        // ワールド座標での正しい向きを返す
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