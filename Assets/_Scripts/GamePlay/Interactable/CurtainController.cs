using UnityEngine;

public class CurtainController : MonoBehaviour
{
    [Header("オブジェクト参照")]
    [Tooltip("開閉するカーテンのSpriteRenderer")]
    public SpriteRenderer curtainRenderer;
    [Tooltip("最初に配置されている景色Aのオブジェクト")]
    public Draggable sceneryA;
    [Tooltip("景色が置かれるスロットのDropZone")]
    public DropZone sceneryDropZone; // ObjectSlotからDropZoneに変更

    [Header("スプライト設定")]
    public Sprite closedCurtainSprite;
    public Sprite openCurtainSprite;

    // --- 内部変数 ---
    private ObjectSlot scenerySlot;
    private bool isOpen = false;
    private Collider2D sceneryACollider;
    private bool isSceneryB_Placed = false;

    void Start()
    {
        // 起動時に、設定されたDropZoneから正しいObjectSlotデータを取得する
        if (sceneryDropZone != null)
        {
            scenerySlot = sceneryDropZone.associatedSlot;
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
        // --- クリック判定ロジック ---
        if (Input.GetMouseButtonDown(0))
        {
            if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

            if (ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                {
                    isOpen = !isOpen;
                    UpdateCurtainState();
                }
            }
        }

        // --- 景色Bが配置されたかのチェック（一度だけ実行） ---
        if (!isSceneryB_Placed && scenerySlot != null && scenerySlot.IsOccupied())
        {
            // スロットにSceneryBが配置された瞬間を検知
            if (scenerySlot.currentObject.itemType == ItemType.SceneryB)
            {
                Collider2D sceneryBCollider = scenerySlot.currentObject.GetComponent<Collider2D>();
                if (sceneryBCollider != null)
                {
                    sceneryBCollider.enabled = false;
                    isSceneryB_Placed = true; // フラグを立てて、この処理を二度と実行しないようにする
                    Debug.Log("景色Bが配置されたため、当たり判定を無効化しました。");
                }
            }
        }
    }

    private void UpdateCurtainState()
    {
        if (curtainRenderer == null) return;

        // カーテンの見た目を更新
        curtainRenderer.sprite = isOpen ? openCurtainSprite : closedCurtainSprite;

        // 景色Aが存在する場合のみ、その操作可否を切り替える
        if (sceneryA != null && sceneryACollider != null)
        {
            sceneryA.enabled = isOpen;
            sceneryACollider.enabled = isOpen;
        }
    }
}