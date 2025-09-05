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
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += PlayBGMForScene;
        DetectionManager.OnGameOver += StopMusic;
    }

    private void OnDisable()
    {
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