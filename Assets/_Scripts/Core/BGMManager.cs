using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("BGM設定")]
    [Tooltip("ゲーム内で使用するBGM設定ファイルのリスト")]
    public List<BGMData> bgmDataList;

    private AudioSource audioSource;
    private string currentSceneName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            // AudioSourceの取得と、nullチェックをここで行う
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // エラーの原因が明確にわかるように、ログを出して処理を停止する
                Debug.LogError("BGMManagerにAudioSourceコンポーネントがアタッチされていません！ Inspectorを確認してください。", this.gameObject);
                this.enabled = false; // スクリプトを無効化して、これ以上エラーが出ないようにする
                return;
            }
            audioSource.loop = true;
        }
        else if (Instance != this)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnEnable()
    {
        // 自分がシングルトンの本物インスタンスでない場合は、
        // 何もせずに即座に処理を終了する
        if (Instance != null && Instance != this) return;

        SceneManager.sceneLoaded += PlayBGMForScene;
        DetectionManager.OnGameOver += StopMusic;
    }

    private void OnDisable()
    {
        // 自分がシングルトンの本物インスタンスでない場合は、
        // 何もせずに即座に処理を終了する
        if (Instance != null && Instance != this) return;

        SceneManager.sceneLoaded -= PlayBGMForScene;
        DetectionManager.OnGameOver -= StopMusic;
    }

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

            // playOnLoadフラグがtrueの場合のみ、自動で再生
            if (dataToPlay.playOnLoad)
            {
                audioSource.Play();
            }
            // falseの場合は、クリップをセットした状態で待機
        }
        else
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    // ▼▼▼ このメソッドを追加 ▼▼▼
    /// <summary>
    /// 待機中のBGMの再生を開始します。
    /// </summary>
    public void TriggerBGMPlayback()
    {
        // クリップがセットされていて、かつ再生中でない場合のみ再生
        if (audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}