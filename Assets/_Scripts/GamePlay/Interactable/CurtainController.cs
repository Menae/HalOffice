using UnityEngine;

/// <summary>
/// カーテンの表示と相互作用を管理するコンポーネント。
/// カーテンの開閉に応じてスプライト切替と、景色オブジェクト／スロットの操作可否を同期する。
/// </summary>
public class CurtainController : MonoBehaviour
{
    [Header("オブジェクト参照")]
    [Tooltip("開閉するカーテンのSpriteRenderer")]
    /// <summary>
    /// 開閉するカーテンのSpriteRenderer。InspectorでD&D。nullチェックあり。
    /// </summary>
    public SpriteRenderer curtainRenderer;

    [Tooltip("最初に配置されている景色Aのオブジェクト")]
    /// <summary>
    /// 最初に配置される景色AのDraggable。InspectorでD&D。カーテンの開閉に合わせて操作可否を切替。
    /// </summary>
    public Draggable sceneryA;

    [Tooltip("景色が置かれるスロットのDropZone")]
    /// <summary>
    /// 景色が置かれるDropZone。InspectorでD&D。GameSlotの場合はassociatedSlotからObjectSlotを取得して使用。
    /// </summary>
    public DropZone sceneryDropZone;

    [Header("スプライト設定")]
    /// <summary>
    /// 閉じているときに表示するスプライト。InspectorでD&D。null時は描画更新をスキップ。
    /// </summary>
    public Sprite closedCurtainSprite;

    /// <summary>
    /// 開いているときに表示するスプライト。InspectorでD&D。null時は描画更新をスキップ。
    /// </summary>
    public Sprite openCurtainSprite;

    // 内部変数
    /// <summary>DropZoneから取得した対応するObjectSlotのキャッシュ。</summary>
    private ObjectSlot scenerySlot;

    /// <summary>カーテンの状態フラグ（true=開いている）。</summary>
    private bool isOpen = false;

    /// <summary>景色AのCollider2D参照をキャッシュ。nullチェックあり。</summary>
    private Collider2D sceneryACollider;

    /// <summary>SceneryBが配置されたことを一度だけ検知するためのフラグ。</summary>
    private bool isSceneryB_Placed = false;

    /// <summary>DropZoneのCollider2D参照をキャッシュ。nullチェックあり。</summary>
    private Collider2D sceneryDropZoneCollider;

    /// <summary>
    /// UnityのStartイベント。スクリプト有効化後、最初のフレームの直前に呼ばれる。
    /// DropZoneからassociatedSlotとCollider2Dを取得してキャッシュし、景色Aのコライダーをキャッシュして初期状態を反映。
    /// 設定漏れ時はエラーログを出力。
    /// </summary>
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

    /// <summary>
    /// 毎フレーム呼ばれるUpdateイベント。
    /// 右クリックによるカーテンの開閉判定と、スロットにSceneryBが配置された瞬間の処理（1回のみ）を管理。
    /// 入力が無効化されている場合は早期リターン。
    /// </summary>
    void Update()
    {
        // 右クリックでカーテンの開閉を切替
        if (Input.GetMouseButtonDown(1))
        {
            if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

            if (ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

                // クリック地点のヒットに自オブジェクトが含まれているか判定
                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                    {
                        isOpen = !isOpen;
                        UpdateCurtainState();
                        break;
                    }
                }
            }
        }

        // SceneryBがスロットに配置された瞬間を一度だけ検知して処理
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

    /// <summary>
    /// カーテンのスプライトと関連オブジェクトの操作可能状態を同期する。
    /// curtainRendererが未設定の場合は処理を中止。sceneryAとDropZoneのコライダーを開閉状態に合わせて有効/無効化。
    /// </summary>
    private void UpdateCurtainState()
    {
        if (curtainRenderer == null) return;

        curtainRenderer.sprite = isOpen ? openCurtainSprite : closedCurtainSprite;

        // 景色Aの操作可否をカーテン状態に合わせて切替
        if (sceneryA != null && sceneryACollider != null)
        {
            sceneryA.enabled = isOpen;
            sceneryACollider.enabled = isOpen;
        }

        // 景色スロット（DropZone）の当たり判定をカーテン状態に合わせて切替
        if (sceneryDropZoneCollider != null)
        {
            sceneryDropZoneCollider.enabled = isOpen;
        }
    }
}