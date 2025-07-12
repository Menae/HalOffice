using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Listを使うために必要

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
        // シングルトンの実装（シーンをまたいで存在し続ける）
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true; // BGMは常にループさせる
    }

    private void OnEnable()
    {
        // シーンがロードされた時に、PlayBGMForSceneメソッドを呼ぶように登録
        SceneManager.sceneLoaded += PlayBGMForScene;
        // DetectionManagerからのゲームオーバー通知を受け取るように登録
        DetectionManager.OnGameOver += StopMusic;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= PlayBGMForScene;
        // 登録を解除（お作法）
        DetectionManager.OnGameOver -= StopMusic;
    }

    // シーンがロードされた時に自動的に呼ばれるメソッド
    private void PlayBGMForScene(Scene scene, LoadSceneMode mode)
    {
        // ロードされたシーンの名前を取得
        string sceneName = scene.name;

        // もし、前回と同じシーンなら（リロードなど）、何もしない
        if (sceneName == currentSceneName) return;

        currentSceneName = sceneName;

        // BGMデータリストの中から、現在のシーン名に一致するものを探す
        BGMData dataToPlay = bgmDataList.Find(data => data.sceneName == sceneName);

        if (dataToPlay != null)
        {
            // 見つかったら、BGMを再生
            audioSource.clip = dataToPlay.bgmClip;
            audioSource.volume = dataToPlay.volume;
            audioSource.Play();
        }
        else
        {
            // 見つからなければ、BGMを停止
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }
}