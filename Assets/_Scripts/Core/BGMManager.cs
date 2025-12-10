using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
/// <summary>
/// シーンに応じたBGMの選択と再生を管理するシングルトンコンポーネント。
/// </summary>
/// <remarks>
/// Awakeでシングルトンを確立し、__DontDestroyOnLoad__で永続化する。
/// Sceneのロードに応じてBGMを切り替え、ゲームオーバー時の停止を管理する。
/// </remarks>
public class BGMManager : MonoBehaviour
{
    /// <summary>
    /// グローバルにアクセス可能なBGMManagerの唯一インスタンス。
    /// </summary>
    /// <remarks>
    /// シングルトンパターン。複数存在した場合は後から生成されたオブジェクトを破棄する。
    /// </remarks>
    public static BGMManager Instance { get; private set; }

    [Header("BGM設定")]
    [Tooltip("ゲーム内で使用するBGM設定ファイルのリスト")]
    /// <summary>
    /// Scene名とBGMを紐付けたデータのリスト。
    /// </summary>
    /// <remarks>
    /// InspectorでD&Dして設定。nullや空リストの場合は再生なし。
    /// </remarks>
    public List<BGMData> bgmDataList;

    private AudioSource audioSource;
    private string currentSceneName;

    /// <summary>
    /// 初期化処理。Awakeはスクリプトの初期化順で早い段階で呼ばれるため、
    /// シングルトン確立とコンポーネントチェックをここで行う。
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            // AudioSourceコンポーネントを取得し、存在しない場合はスクリプトを無効化して以後のエラーを防ぐ。
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("BGMManagerにAudioSourceコンポーネントがアタッチされていません！ Inspectorを確認してください。", this.gameObject);
                this.enabled = false;
                return;
            }
            audioSource.loop = true;
        }
        else if (Instance != this)
        {
            // 既存の正しいインスタンスが存在する場合、重複インスタンスは破棄する。
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 有効化時にシーンロードとゲームオーバーイベントを購読する。
    /// </summary>
    /// <remarks>
    /// OnEnableはオブジェクトがアクティブになった直後に呼ばれる。インスタンスが本物でない場合は購読しない。
    /// </remarks>
    private void OnEnable()
    {
        if (Instance != null && Instance != this) return;

        SceneManager.sceneLoaded += PlayBGMForScene;
        DetectionManager.OnGameOver += StopMusic;
    }

    /// <summary>
    /// 無効化時に購読解除を行う。
    /// </summary>
    /// <remarks>
    /// OnDisableはオブジェクトが非アクティブになる直前に呼ばれる。イベントの二重購読を防ぐため必ず解除する。
    /// </remarks>
    private void OnDisable()
    {
        if (Instance != null && Instance != this) return;

        SceneManager.sceneLoaded -= PlayBGMForScene;
        DetectionManager.OnGameOver -= StopMusic;
    }

    /// <summary>
    /// シーンロード時に対応するBGMをセットし、必要に応じて再生する。
    /// </summary>
    /// <param name="scene">ロードされたシーン情報。</param>
    /// <param name="mode">シーンロードモード。</param>
    /// <remarks>
    /// 同一シーンかつ既に再生中であれば何もしない。該当データが無ければBGMを停止してクリップを解除する。
    /// </remarks>
    private void PlayBGMForScene(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        if (sceneName == currentSceneName && audioSource.isPlaying) return;
        currentSceneName = sceneName;

        BGMData dataToPlay = bgmDataList.Find(data => data.sceneName == sceneName);

        if (dataToPlay != null)
        {
            audioSource.clip = dataToPlay.bgmClip;
            audioSource.volume = dataToPlay.volume;

            // playOnLoadが有効な場合のみ自動再生。
            if (dataToPlay.playOnLoad)
            {
                audioSource.Play();
            }
            // playOnLoadがfalseの場合はクリップをセットしたまま待機する。
        }
        else
        {
            // 対応するBGMが見つからない場合は再生停止してクリップを解除する。
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    /// <summary>
    /// 再生中のBGMを即時停止する。
    /// </summary>
    /// <remarks>
    /// ゲームオーバーなど、強制停止が必要な場面で呼び出す。clipは解除しないため、再開時は同じクリップを再生可能。
    /// </remarks>
    public void StopMusic()
    {
        audioSource.Stop();
    }

    /// <summary>
    /// クリップがセットされている待機中のBGMを開始する。
    /// </summary>
    /// <remarks>
    /// clipがnullの場合や既に再生中の場合は何もしない。ユーザー操作やイベントから遅延再生をトリガーするためのメソッド。
    /// </remarks>
    public void TriggerBGMPlayback()
    {
        if (audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}