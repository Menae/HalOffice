using UnityEngine;

public class TVController : MonoBehaviour
{
    [Header("監視対象")]
    [Tooltip("このゲーム機が破棄されたかを監視します")]
    public Draggable gameConsole;

    [Header("表示するスクリーン")]
    [Tooltip("ゲーム画面やニュース画面を表示するSpriteRenderer")]
    public SpriteRenderer screenRenderer;

    [Header("スプライト設定")]
    [Tooltip("通常時に表示するゲーム画面のスプライト")]
    public Sprite gameScreenSprite;

    [Tooltip("ゲーム機が破棄された後に表示するニュース画面のスプライト")]
    public Sprite newsScreenSprite;

    // --- 内部変数 ---
    private bool isGameConsoleDestroyed = false; // ゲーム機が破棄されたかどうかのフラグ
    private bool isPowerOn = true; // テレビの電源がONかどうかの状態を記憶

    void Start()
    {
        // ゲーム開始時の見た目を更新
        UpdateScreenDisplay();
    }

    void Update()
    {
        // --- クリック判定ロジック ---
        // 1. 左クリックされた瞬間かチェック
        if (Input.GetMouseButtonDown(0))
        {
            // 入力が無効な場合は何もしない
            if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

            // 2. ScreenToWorldConverterを使って、マウス座標をゲーム世界の座標に変換
            if (ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                // 3. 変換した座標に何があるかRaycastで調べる
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

                // 4. もし何かがあり、それがこのTV自身だったら
                if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                {
                    // 電源の状態を切り替える
                    TogglePower();
                }
            }
        }

        // --- ゲーム機の監視ロジック（変更なし） ---
        if (!isGameConsoleDestroyed && gameConsole == null)
        {
            isGameConsoleDestroyed = true;
            Debug.Log("ゲーム機が破棄されたため、ニュース画面に切り替えます。");
            UpdateScreenDisplay();
        }
    }

    /// <summary>
    /// 電源の状態を反転させ、見た目を更新する
    /// </summary>
    private void TogglePower()
    {
        isPowerOn = !isPowerOn;
        UpdateScreenDisplay();
    }

    /// <summary>
    /// 現在の状態に基づいて、スクリーンの表示を更新する
    /// </summary>
    private void UpdateScreenDisplay()
    {
        if (screenRenderer == null) return;

        if (!isPowerOn)
        {
            screenRenderer.color = Color.black;
            return;
        }

        screenRenderer.color = Color.white;
        screenRenderer.sprite = isGameConsoleDestroyed ? newsScreenSprite : gameScreenSprite;
    }
}