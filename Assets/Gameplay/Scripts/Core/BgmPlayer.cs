using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmPlayer : MonoBehaviour {

    public static BgmPlayer Instance;

    [Header("Sources")]
    public AudioSource baseSource;
    public AudioSource layerSource;

    [Header("Clips")]
    public AudioClip baseTrack;
    public AudioClip layerTrack;

    [Range(0f, 1f)] public float baseVolume = 0.6f;
    [Range(0f, 1f)] public float layerVolume = 0.6f;

    public bool loop = true;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (baseSource == null)
            baseSource = gameObject.AddComponent<AudioSource>();

        if (layerSource == null)
            layerSource = gameObject.AddComponent<AudioSource>();

        SetupSource(baseSource, baseVolume);
        SetupSource(layerSource, layerVolume);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start() {
        PlayBoth();
    }

    void SetupSource(AudioSource src, float vol) {
        src.playOnAwake = false;
        src.loop = loop;
        src.volume = vol;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        PlayBoth();
    }

    void PlayBoth() {
        if (baseTrack != null) {
            baseSource.clip = baseTrack;
            baseSource.time = 0f;
            baseSource.Play();
        }

        if (layerTrack != null) {
            layerSource.clip = layerTrack;
            layerSource.time = 0f;
            layerSource.Play();
        }
    }
}