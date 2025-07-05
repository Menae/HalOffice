using UnityEngine;
using System.Collections;

public class NPCMove_v1 : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 1f;
    [Header("歩く時間（秒）")]
    public float walkTime = 1.5f;
    [Header("待機時間の範囲（秒）")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;
    [Header("視界設定")]
    public float viewRadius = 5f;
    [Range(0, 360)]
    public float viewAngle = 90f;

    private SpriteRenderer spriteRenderer;
    private bool isFacingRight = true;
    public bool isCursorInView { get; private set; }
    private FOVRenderer fovRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        fovRenderer = GetComponentInChildren<FOVRenderer>();
        if (fovRenderer == null)
        {
            GameObject fovObject = new GameObject("FieldOfView");
            fovObject.transform.SetParent(transform, false);
            fovRenderer = fovObject.AddComponent<FOVRenderer>();
        }
        StartCoroutine(NPCBehaviorRoutine());
    }

    void Update()
    {
        CheckFieldOfView();

        // fovRendererがセットされていれば、パラメータを渡す
        if (fovRenderer != null)
        {
            // 半径と角度の情報だけを渡す
            fovRenderer.SetViewParameters(viewAngle, viewRadius);
        }
    }

    private IEnumerator NPCBehaviorRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
            Flip();
            float walkTimer = 0f;
            while (walkTimer < walkTime)
            {
                transform.Translate(GetDirection() * moveSpeed * Time.deltaTime);
                walkTimer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void CheckFieldOfView()
    {
        Vector3 cursorPos = Input.mousePosition;
        float distance_from_camera = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);
        cursorPos.z = distance_from_camera;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(cursorPos);
        float distanceToCursor = Vector3.Distance(transform.position, mouseWorldPos);
        Vector3 directionToCursor = (mouseWorldPos - transform.position).normalized;
        if (Vector3.Angle(GetDirection(), directionToCursor) < viewAngle / 2)
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
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
    }

    private Vector3 GetDirection()
    {
        return isFacingRight ? Vector3.right : Vector3.left;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Vector3 worldPosition = transform.position;
        Gizmos.DrawWireSphere(worldPosition, viewRadius);

        int segments = 50;
        float angleStep = viewAngle / segments;
        float firstAngle = -viewAngle / 2;
        Vector3 firstLocalPoint = Quaternion.Euler(0, 0, firstAngle) * Vector3.right * viewRadius;
        Vector3 firstWorldPoint = transform.TransformPoint(firstLocalPoint);
        Gizmos.DrawLine(worldPosition, firstWorldPoint);

        Vector3 prevWorldPoint = firstWorldPoint;
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -viewAngle / 2 + angleStep * i;
            Vector3 localPoint = Quaternion.Euler(0, 0, currentAngle) * Vector3.right * viewRadius;
            Vector3 worldPoint = transform.TransformPoint(localPoint);
            Gizmos.DrawLine(prevWorldPoint, worldPoint);
            prevWorldPoint = worldPoint;
        }
        Gizmos.DrawLine(worldPosition, prevWorldPoint);

        if (isCursorInView)
        {
            Gizmos.color = Color.green;
            Vector3 cursorPos = Input.mousePosition;
            cursorPos.z = worldPosition.z;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(cursorPos);
            Gizmos.DrawLine(worldPosition, mouseWorldPos);
        }
    }
}