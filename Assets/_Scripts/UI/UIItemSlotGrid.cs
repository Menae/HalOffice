using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIItemSlotGrid : MonoBehaviour
{
    // -----------------------------
    // References
    // -----------------------------
    [Header("参照")]
    [Tooltip("このスロット一覧をスクロールさせる ScrollRect。未指定なら親から自動取得します。")]
    public ScrollRect scrollRect;

    [Tooltip("ScrollRect の Content。未指定なら scrollRect.content、なければ自分自身の RectTransform を使用します。")]
    public RectTransform content;

    [Tooltip("縦スクロール用の Scrollbar（任意）。割り当てると連動します。")]
    public Scrollbar verticalScrollbar;

    // -----------------------------
    // Spawning
    // -----------------------------
    [Header("スロット生成")]
    [Tooltip("スロットのプレハブ（ルート）。UIDraggable は子に付いていてOKです（例：IconEach）。")]
    public GameObject slotPrefabRoot;

    [Tooltip("生成するスロットの合計数。")]
    [Min(0)] public int totalSlots = 10;

    // -----------------------------
    // Layout
    // -----------------------------
    [Header("レイアウト")]
    [Tooltip("1 行に並べるスロット数（列数）。")]
    [Min(1)] public int itemsPerRow = 5;

    [Tooltip("スロット同士の間隔（x=左右, y=上下）。")]
    public Vector2 spacing = new Vector2(8, 8);

    [Tooltip("外枠の余白（左・右・上・下）。※ コンストラクタで生成せず、Awake/OnValidate で初期化します。")]
    public RectOffset padding;  // Awake/OnValidateで生成

    [Tooltip("セルサイズをプレハブ（ルート）の RectTransform から自動取得する。")]
    public bool autoCellSizeFromPrefab = true;

    [Tooltip("Viewport の幅に合わせて、列数固定のままセル幅を自動フィットする。")]
    public bool fitCellWidthToViewport = true;

    [Tooltip("セルサイズを手動指定する場合のサイズ（自動がOFFのとき有効）。")]
    public Vector2 manualCellSize = new Vector2(64, 64);

    // -----------------------------
    // Behavior
    // -----------------------------
    [Header("動作設定")]
    [Tooltip("再生開始時に自動で再生成します。")]
    public bool rebuildOnStart = true;

    [Tooltip("再生成時に Content の子オブジェクトを全てクリアします。")]
    public bool clearBeforeBuild = true;

    [Tooltip("マウスホイールのスクロール感度。")]
    public float scrollSensitivity = 30f;

    // -----------------------------
    // Compat / Debug
    // -----------------------------
    [Header("UIDraggable 互換オプション（任意）")]
    [Tooltip("ドラッグ中に blocksRaycasts を切り替える想定のため、足りなければ CanvasGroup を自動で付与します。")]
    public bool autoAddCanvasGroupForDrag = true;

    [Header("デバッグ")]
    [Tooltip("再生成時に原因特定用のログを出力します。")]
    public bool debugLogs = true;
    [Tooltip("生成直後に可視化の保険（強制Active、最小限のデバッグ背景）を入れます。問題切り分け用。")]
    public bool debugEnsureVisible = true;

    private GridLayoutGroup grid;
    private bool builtOnceAtRuntime = false;
    private bool deferredLayoutScheduled = false;

    // -----------------------------
    // Lifecycle
    // -----------------------------
    void Reset()
    {
        ResolveReferences();
        EnsurePadding();
    }

    void Awake()
    {
        ResolveReferences();
        EnsurePadding();
        EnsureGrid();
    }

    void OnEnable()
    {
        if (Application.isPlaying && !builtOnceAtRuntime)
        {
            ResolveReferences();
            EnsurePadding();
            EnsureGrid();

            if (content != null && content.childCount == 0 &&
                slotPrefabRoot != null)
            {
                Rebuild();
            }
        }
    }

    void Start()
    {
        if (rebuildOnStart && !builtOnceAtRuntime)
            Rebuild();
    }

    void OnValidate()
    {
        // OnValidate内での即時レイアウト変更はUnityの禁止事項のため、
        // エディタの次の描画タイミングまで処理を遅延させます。
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            // 遅延実行される頃にはオブジェクトが削除されている可能性への対策
            if (this == null || gameObject == null) return;

            EnsurePadding();
            ResolveReferences();
            EnsureGrid();
            ApplyLayoutOnly();
        };
#endif
    }

    // -----------------------------
    // Public
    // -----------------------------
    [ContextMenu("再生成 (Rebuild)")]
    public void Rebuild()
    {
        ResolveReferences();
        EnsurePadding();
        EnsureGrid();

        if (content == null)
        {
            if (debugLogs) Debug.LogWarning("[UIItemSlotGrid] content が未解決のため生成を中止しました。", this);
            return;
        }
        if (slotPrefabRoot == null)
        {
            if (debugLogs) Debug.LogWarning("[UIItemSlotGrid] slotPrefabRoot が設定されていません。生成を中止します。", this);
            return;
        }

        // ---------------------------------------------------------
        // 1. 強制クリアモードの場合（手動設定も消えます）
        // ---------------------------------------------------------
        if (clearBeforeBuild)
        {
            for (int i = content.childCount - 1; i >= 0; --i)
            {
                var child = content.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        // ---------------------------------------------------------
        // 2. 差分更新ロジック（既存を守りつつ増減）
        // ---------------------------------------------------------
        int currentCount = content.childCount;
        int targetCount = totalSlots;

        // A. 足りない場合：不足分だけ追加生成
        if (currentCount < targetCount)
        {
            int addCount = targetCount - currentCount;
            for (int i = 0; i < addCount; i++)
            {
                var go = Instantiate(slotPrefabRoot, content);

                // 万一プレハブが非アクティブだった場合の保険
                if (!go.activeSelf) go.SetActive(true);

                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.anchoredPosition3D = Vector3.zero;
                }

                // UIDraggable 用のCanvasGroup自動付与（既存）
                if (autoAddCanvasGroupForDrag)
                {
                    var draggable = go.GetComponentInChildren<UIDraggable>(true);
                    if (draggable != null && draggable.GetComponent<CanvasGroup>() == null)
                    {
                        draggable.gameObject.AddComponent<CanvasGroup>();
                    }
                }

                // —— 可視化の保険（デバッグ表示用） ——
                if (debugEnsureVisible)
                {
                    bool hasRenderable = false;
                    var graphics = go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                    foreach (var g in graphics)
                    {
                        if (g is UnityEngine.UI.Image img)
                        {
                            if (img.sprite != null && img.color.a > 0f && img.enabled) { hasRenderable = true; break; }
                        }
                        else if (g.color.a > 0f && g.enabled)
                        {
                            hasRenderable = true; break;
                        }
                    }

                    if (!hasRenderable)
                    {
                        var dbgGO = new GameObject("_DebugBG", typeof(UnityEngine.UI.Image));
                        dbgGO.transform.SetParent(go.transform, false);
                        var dbgImg = dbgGO.GetComponent<UnityEngine.UI.Image>();
                        dbgImg.raycastTarget = false;
#if UNITY_EDITOR
                        dbgImg.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
#endif
                        dbgImg.color = new Color(1f, 0f, 1f, 0.4f);

                        var dbgRT = dbgGO.GetComponent<RectTransform>();
                        dbgRT.anchorMin = Vector2.zero;
                        dbgRT.anchorMax = Vector2.one;
                        dbgRT.offsetMin = Vector2.zero;
                        dbgRT.offsetMax = Vector2.zero;
                    }
                }
            }
        }
        // B. 多すぎる場合：末尾から削除
        else if (currentCount > targetCount)
        {
            int removeCount = currentCount - targetCount;
            for (int i = 0; i < removeCount; i++)
            {
                // 末尾（最新に追加されたもの）から削除していくことで、
                // 前の方にある（手動設定された可能性が高い）スロットを守る
                var child = content.GetChild(content.childCount - 1);
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        // ---------------------------------------------------------
        // 3. スクロール設定とレイアウト更新
        // ---------------------------------------------------------
        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = scrollSensitivity;
            if (verticalScrollbar != null)
                scrollRect.verticalScrollbar = verticalScrollbar;
        }

        // 直後は Viewport サイズが未確定なことがあるので、次フレームで再レイアウト
        if (Application.isPlaying) StartDeferredLayout();
        else ApplyLayoutOnly(); // エディタ上なら即時反映

        builtOnceAtRuntime = true;
        if (debugLogs) Debug.Log($"[UIItemSlotGrid] Updated slots. Current Total: {content.childCount}", this);
    }

    // Optional: push item dataset into slots
    public void PopulateItems(IList<ItemData> items)
    {
        int childCount = content != null ? content.childCount : 0;
        int count = Mathf.Min(items.Count, childCount);
        for (int i = 0; i < count; i++)
        {
            var child = content.GetChild(i);
            var draggable = child.GetComponentInChildren<UIDraggable>(true);
            if (draggable != null)
            {
                draggable.itemData = items[i];
            }
        }
    }

    // -----------------------------
    // Internal helpers
    // -----------------------------
    private void ResolveReferences()
    {
        if (scrollRect == null)
            scrollRect = GetComponentInParent<ScrollRect>(true);

        if (content == null)
        {
            if (scrollRect != null && scrollRect.content != null)
                content = scrollRect.content;
            else
                content = GetComponent<RectTransform>(); // fallback
        }

        if (content != null)
        {
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
        }
    }

    private void EnsurePadding()
    {
        if (padding == null)
            padding = new RectOffset(8, 8, 8, 8);
    }

    private void EnsureGrid()
    {
        if (content == null) return;

        grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
    }

    public void ApplyLayoutOnly()
    {
        ApplyLayoutOnlyInternal(force: false);
    }

    private void ApplyLayoutOnlyInternal(bool force)
    {
        if (content == null || grid == null) return;

        // 1) 基本セルサイズ
        Vector2 cell = manualCellSize;

        // 2) プレハブから自動取得（stretch対策フォールバック）
        if (autoCellSizeFromPrefab && slotPrefabRoot != null)
        {
            var rt = slotPrefabRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 cand = rt.sizeDelta;
                if (cand.x <= 0f || cand.y <= 0f) cand = rt.rect.size;
                if (cand.x > 0f) cell.x = cand.x;
                if (cand.y > 0f) cell.y = cand.y;
            }
        }

        // 3) 幅フィット：Viewport の幅が未確定なら次フレームに再実行
        if (fitCellWidthToViewport)
        {
            float viewportW = GetViewportWidth();
            if (viewportW > 1f)
            {
                float totalSpace = spacing.x * (itemsPerRow - 1);
                float totalPad = (padding != null ? padding.left + padding.right : 0);
                float w = (viewportW - totalSpace - totalPad) / Mathf.Max(1, itemsPerRow);
                cell.x = Mathf.Max(1f, w);
            }
            else if (!deferredLayoutScheduled && Application.isPlaying && !force)
            {
                StartDeferredLayout();
            }
        }

        // 4) 高さフォールバック（ゼロを許さない）
        if (cell.y <= 0f)
        {
            if (manualCellSize.y > 0f) cell.y = manualCellSize.y;
            else if (cell.x > 0f) cell.y = cell.x;
            else cell.y = 64f;
        }

        // 5) GridLayoutGroup に反映
        grid.cellSize = new Vector2(Mathf.Max(1f, cell.x), Mathf.Max(1f, cell.y));
        grid.spacing = spacing;
        grid.padding = padding ?? new RectOffset();
        grid.constraintCount = Mathf.Max(1, itemsPerRow);

        // 6) Content の高さを算出して設定
        int cols = Mathf.Max(1, itemsPerRow);
        int count = Mathf.Max(totalSlots, content.childCount);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / cols));

        float padV = (padding != null ? padding.top + padding.bottom : 0);
        float height = padV + rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * spacing.y;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private float GetViewportWidth()
    {
        float width = 0f;
        if (scrollRect != null && scrollRect.viewport != null)
            width = scrollRect.viewport.rect.width;

        if (width <= 1f)
        {
            var p = content != null ? content.parent as RectTransform : null;
            if (p != null) width = p.rect.width;
        }
        if (width <= 1f)
        {
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null && scrollRect.viewport != null)
                width = scrollRect.viewport.rect.width;
        }
        return width;
    }

    private void StartDeferredLayout()
    {
        if (deferredLayoutScheduled) return;
        StartCoroutine(DeferredLayoutRoutine());
    }

    private IEnumerator DeferredLayoutRoutine()
    {
        deferredLayoutScheduled = true;
        yield return null; // wait one frame
        Canvas.ForceUpdateCanvases();
        ApplyLayoutOnlyInternal(force: true);
        deferredLayoutScheduled = false;
    }
}
