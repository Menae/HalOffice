using UnityEngine;

/// <summary>
/// カーテンの表示と相互作用を管理するコンポーネント。
/// クリック判定とドラッグ判定を区別し、適切な挙動（開閉またはドラッグ優先）を行う。
/// </summary>
public class CurtainController : MonoBehaviour
{
    [Header("オブジェクト参照")]
    [Tooltip("開閉するカーテンのSpriteRenderer")]
    public SpriteRenderer curtainRenderer;

    [Tooltip("最初に配置されている景色Aのオブジェクト")]
    public Draggable sceneryA;

    [Tooltip("景色が置かれるスロットのDropZone")]
    public DropZone sceneryDropZone;

    [Header("スプライト設定")]
    public Sprite closedCurtainSprite;
    public Sprite openCurtainSprite;

    // 内部変数
    private ObjectSlot scenerySlot;
    private bool isOpen = false;
    private Collider2D sceneryACollider;
    private bool isSceneryB_Placed = false;
    private Collider2D sceneryDropZoneCollider;

    // クリック/ドラッグ判定用
    private Vector3 pointerDownPos;
    private bool isPointerDown = false;
    private bool isDragThresholdExceeded = false;
    private const float DragThreshold = 10f; // ドラッグとみなす移動距離（ピクセル）

    void Start()
    {
        if (sceneryDropZone != null)
        {
            scenerySlot = sceneryDropZone.associatedSlot;
            sceneryDropZoneCollider = sceneryDropZone.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogError("CurtainControllerにsceneryDropZoneが設定されていません！", this.gameObject);
        }

        if (sceneryA != null)
        {
            sceneryACollider = sceneryA.GetComponent<Collider2D>();
        }
        UpdateCurtainState();
    }

    void Update()
    {
        // 入力無効時は処理しない
        if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

        // 1. マウスボタンを押した瞬間
        if (Input.GetMouseButtonDown(0))
        {
            if (ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                // カーテンをクリックしたか確認
                RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);
                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                    {
                        // カーテン上で押下開始
                        isPointerDown = true;
                        pointerDownPos = Input.mousePosition;
                        isDragThresholdExceeded = false;
                        break;
                    }
                }
            }
        }

        // 2. マウスボタンを押している間
        if (isPointerDown)
        {
            // 移動距離をチェック
            if (Vector3.Distance(Input.mousePosition, pointerDownPos) > DragThreshold)
            {
                isDragThresholdExceeded = true;
            }

            // 3. マウスボタンを離した瞬間
            if (Input.GetMouseButtonUp(0))
            {
                // ドラッグ操作（移動）が行われていなければ「クリック」とみなして開閉
                if (!isDragThresholdExceeded)
                {
                    isOpen = !isOpen;
                    UpdateCurtainState();
                }

                // 状態リセット
                isPointerDown = false;
            }
        }
        // マウスが離れた、または外れた場合の安全策（Upを拾い損ねた場合など）
        else if (Input.GetMouseButtonUp(0))
        {
            isPointerDown = false;
        }

        // SceneryB配置検知（変更なし）
        if (!isSceneryB_Placed && scenerySlot != null && scenerySlot.IsOccupied())
        {
            if (scenerySlot.currentObject.itemData != null && scenerySlot.currentObject.itemData.itemType == ItemType.SceneryB)
            {
                Collider2D sceneryBCollider = scenerySlot.currentObject.GetComponent<Collider2D>();
                if (sceneryBCollider != null)
                {
                    sceneryBCollider.enabled = false;
                    isSceneryB_Placed = true;
                    Debug.Log("景色Bが配置されたため、当たり判定を無効化しました。");
                }
            }
        }
    }

    private void UpdateCurtainState()
    {
        if (curtainRenderer == null) return;

        curtainRenderer.sprite = isOpen ? openCurtainSprite : closedCurtainSprite;

        // 景色Aの操作可否
        if (sceneryA != null && sceneryACollider != null)
        {
            sceneryA.enabled = isOpen;
            sceneryACollider.enabled = isOpen;
        }

        // スロットの当たり判定
        if (sceneryDropZoneCollider != null)
        {
            sceneryDropZoneCollider.enabled = isOpen;
        }
    }
}