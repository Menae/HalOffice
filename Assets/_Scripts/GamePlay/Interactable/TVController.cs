using UnityEngine;

/// <summary>
/// テレビの表示管理とユーザー操作の受け付けを行うコンポーネント。
/// Inspectorで表示するSpriteや監視対象を設定し、電源操作と表示切替を管理する。
/// </summary>
public class TVController : MonoBehaviour
{
    [Header("監視対象")]
    [Tooltip("このゲーム機が破棄されたかを監視します")]
    /// <summary>
    /// 監視対象のゲーム機。InspectorでD&Dすること。nullになった時点でニュース画面へ切替える。
    /// </summary>
    public Draggable gameConsole;

    [Header("表示するスクリーン")]
    [Tooltip("ゲーム画面やニュース画面を表示するSpriteRenderer")]
    /// <summary>
    /// 表示用のSpriteRenderer。スクリーンの色とスプライトを更新するために使用。未設定時は更新をスキップ。
    /// </summary>
    public SpriteRenderer screenRenderer;

    [Header("スプライト設定")]
    [Tooltip("通常時に表示するゲーム画面のスプライト")]
    /// <summary>
    /// 電源ONかつゲーム機が存在する時に表示するスプライト。Inspectorで設定する。
    /// </summary>
    public Sprite gameScreenSprite;

    [Tooltip("ゲーム機が破棄された後に表示するニュース画面のスプライト")]
    /// <summary>
    /// ゲーム機が破棄された後に表示するスプライト。Inspectorで設定する。
    /// </summary>
    public Sprite newsScreenSprite;

    // 内部状態の保持
    private bool isGameConsoleDestroyed = false; // ゲーム機が破棄されたかどうかのフラグ
    private bool isPowerOn = true; // テレビの電源がONかどうかの状態を記憶

    /// <summary>
    /// UnityのStartイベント。スクリプト有効化後、最初のフレームの直前で呼ばれる。
    /// 初期表示の設定を行う。
    /// </summary>
    private void Start()
    {
        // 起動時のスクリーン表示を初期化する。
        UpdateScreenDisplay();
    }

    /// <summary>
    /// UnityのUpdateイベント。毎フレーム呼ばれる。
    /// クリック操作の受け付けと、監視対象の破棄検知を行う。
    /// </summary>
    private void Update()
    {
        // マウス左クリックの瞬間を検出し、クリック位置がこのTVであれば電源を切り替える。
        if (Input.GetMouseButtonDown(0))
        {
            // 入力が無効な場合は処理を中断する。GameManagerが未設定の場合は入力許可を前提とする。
            if (GameManager.Instance != null && !GameManager.Instance.isInputEnabled) return;

            // マウス画面座標をゲーム世界座標に変換。変換に失敗した場合は中断。
            if (ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                // ワールド座標に対してRaycastを行い、当該位置にあるオブジェクトを取得する。
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

                // 当たったオブジェクトがこのTV自身であれば電源を切り替える。
                if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                {
                    TogglePower();
                }
            }
        }

        // 監視対象が破棄されたかをチェック。破棄を検出したらニュース画面へ切替え。
        if (!isGameConsoleDestroyed && gameConsole == null)
        {
            isGameConsoleDestroyed = true;
            Debug.Log("ゲーム機が破棄されたため、ニュース画面に切り替えます。");
            UpdateScreenDisplay();
        }
    }

    /// <summary>
    /// 電源状態を反転し、表示を更新する。
    /// 電源ON/OFFのトグルを管理し、スクリーンの見た目を反映する。
    /// </summary>
    private void TogglePower()
    {
        isPowerOn = !isPowerOn;
        UpdateScreenDisplay();
    }

    /// <summary>
    /// 現在の内部状態に基づいてスクリーンの色とスプライトを更新する。
    /// screenRendererが未設定の場合は何もしない。
    /// </summary>
    private void UpdateScreenDisplay()
    {
        if (screenRenderer == null) return;

        if (!isPowerOn)
        {
            // 電源OFF時は画面を黒で覆う。
            screenRenderer.color = Color.black;
            return;
        }

        // 電源ON時は白で表示し、ゲーム機の存在有無でスプライトを切替える。
        screenRenderer.color = Color.white;
        screenRenderer.sprite = isGameConsoleDestroyed ? newsScreenSprite : gameScreenSprite;
    }
}