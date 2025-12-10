using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Collider2D))]
public class TelephoneController : MonoBehaviour
{
    /// <summary>
    /// 再生する録音のAudioClip。Inspectorで指定。null時は再生しない。
    /// </summary>
    [Header("オーディオ設定")]
    [Tooltip("右クリックで再生する録音のオーディオクリップ")]
    public AudioClip recordingClip;

    /// <summary>
    /// 録音再生時の音量(0〜1)。Inspectorで設定。
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("録音の音量")]
    public float recordingVolume = 1.0f;

    /// <summary>
    /// 現在録音が再生中かを示す。外部から読み取り可能、内部でのみ更新。
    /// </summary>
    public bool IsPlayingRecording { get; private set; } = false;

    /// <summary>
    /// 使用するAudioSource。Awakeで取得。
    /// </summary>
    private AudioSource audioSource;

    /// <summary>
    /// コンポーネント初期化。AudioSourceを取得し、クリップ・ループ設定・playOnAwakeを設定する。
    /// </summary>
    /// <remarks>
    /// UnityのAwakeはインスタンス生成直後に呼ばれる。Startより前に初期化が必要な処理をここで行う。
    /// </remarks>
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = recordingClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// 毎フレームの入力処理。マウスクリックで電話オブジェクトがヒットしたら録音の再生を切り替える。
    /// </summary>
    /// <remarks>
    /// UnityのUpdateで毎フレーム呼ばれる。ScreenToWorldConverterが存在するかをチェックし、null時は何もしない。
    /// </remarks>
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // ScreenToWorldConverterを使って、正しいワールド座標を取得
            if (ScreenToWorldConverter.Instance != null &&
                ScreenToWorldConverter.Instance.GetWorldPosition(Input.mousePosition, out Vector3 worldPos))
            {
                // 取得した座標に何があるかチェック
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider != null && hit.collider.gameObject == this.gameObject)
                {
                    // 電話がヒットしたら、録音を切り替える
                    ToggleRecording();
                }
            }
        }
    }

    /// <summary>
    /// IsPlayingRecordingをトグルし、再生/停止処理を呼び出す。
    /// </summary>
    private void ToggleRecording()
    {
        IsPlayingRecording = !IsPlayingRecording;

        if (IsPlayingRecording)
        {
            TurnOnRecording();
        }
        else
        {
            TurnOffRecording();
        }
    }

    /// <summary>
    /// 録音再生を開始。再生前に音量を設定する。
    /// </summary>
    private void TurnOnRecording()
    {
        audioSource.volume = recordingVolume; // 再生前に音量を設定
        audioSource.Play();
        Debug.Log("電話の録音を再生します。");
    }

    /// <summary>
    /// 録音再生を停止する。
    /// </summary>
    private void TurnOffRecording()
    {
        audioSource.Stop();
        Debug.Log("電話の録音を停止します。");
    }
}