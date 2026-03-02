using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmPlayer : MonoBehaviour {
    public static BgmPlayer Instance;

    [Header("Audio")]
    public AudioSource source;
    public AudioClip bgm;
    [Range(0f, 1f)] public float volume = 0.6f;
    public bool loop = true;

    [Header("Behavior")]
    public bool restartOnEverySceneLoad = true;

    void Awake() {
        // Singleton
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = loop;
        source.volume = volume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start() {
        // 首次进游戏也播
        PlayFromStart();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (restartOnEverySceneLoad) {
            PlayFromStart();
        } else {
            // 不重启：如果没在播就播
            if (!source.isPlaying) {
                if (source.clip != bgm)
                    source.clip = bgm;
                source.Play();
            }
        }
    }

    void PlayFromStart() {
        if (bgm == null)
            return;
        if (source.clip != bgm)
            source.clip = bgm;

        source.Stop();
        source.time = 0f;
        source.Play();
    }
}