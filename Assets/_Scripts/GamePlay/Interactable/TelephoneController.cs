using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Collider2D))]
public class TelephoneController : MonoBehaviour
{
    [Header("オーディオ設定")]
    [Tooltip("右クリックで再生する録音のオーディオクリップ")]
    public AudioClip recordingClip;
    [Range(0f, 1f)]
    [Tooltip("録音の音量")]
    public float recordingVolume = 1.0f;

    public bool IsPlayingRecording { get; private set; } = false;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = recordingClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                ToggleRecording();
            }
        }
    }

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

    private void TurnOnRecording()
    {
        audioSource.volume = recordingVolume; // 再生前に音量を設定
        audioSource.Play();
        Debug.Log("電話の録音を再生します。");
    }

    private void TurnOffRecording()
    {
        audioSource.Stop();
        Debug.Log("電話の録音を停止します。");
    }
}