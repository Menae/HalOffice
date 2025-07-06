using UnityEngine;
using System.Collections;

// --- このスクリプトはNPCの「動き」と「視界の検知」だけに責任を持ちます ---
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

    public bool isCursorInView { get; private set; }

    // このプロパティを通じて、外部に現在の向きを公開する
    public Vector3 direction { get; private set; } = Vector3.right;

    void Start()
    {
        StartCoroutine(NPCBehaviorRoutine());
    }

    void Update()
    {
        CheckFieldOfView();
    }

    private IEnumerator NPCBehaviorRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
            Flip();
            float walkTimer = 0f;
            while (walkTimer < walkTime)
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
                walkTimer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void CheckFieldOfView()
    {
        Vector3 eyePosition = transform.TransformPoint(eyeOffset);
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        float distanceToCursor = Vector3.Distance(eyePosition, mouseWorldPos);
        Vector3 directionToCursor = (mouseWorldPos - eyePosition).normalized;

        if (distanceToCursor < viewRadius && Vector3.Angle(direction, directionToCursor) < viewAngle / 2)
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
        // ワールド座標の向きを反転
        direction *= -1;
        // 見た目の向きを反転
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 cursorPos = Input.mousePosition;
        float distance_from_camera = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);
        cursorPos.z = distance_from_camera;
        return Camera.main.ScreenToWorldPoint(cursorPos);
    }
}