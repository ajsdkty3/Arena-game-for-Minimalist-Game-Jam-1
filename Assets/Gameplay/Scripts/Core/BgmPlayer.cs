using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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

    [Header("Switch Settings")]
    public float fadeDuration = 0.5f;

    Coroutine _baseFadeRoutine;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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

    // =============================
    // 🔥 NEW: Switch Base Track
    // =============================

    public void SwitchBaseTrack(AudioClip newClip) {
        if (newClip == null)
            return;

        if (baseSource.clip == newClip)
            return;

        if (_baseFadeRoutine != null)
            StopCoroutine(_baseFadeRoutine);

        _baseFadeRoutine = StartCoroutine(FadeSwitchBase(newClip));
    }

    IEnumerator FadeSwitchBase(AudioClip newClip) {

        // Fade out
        float t = 0f;
        float startVol = baseSource.volume;

        while (t < fadeDuration) {
            t += Time.unscaledDeltaTime;
            baseSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
            yield return null;
        }

        baseSource.Stop();
        baseSource.clip = newClip;
        baseSource.time = 0f;
        baseSource.Play();

        // Fade in
        t = 0f;
        while (t < fadeDuration) {
            t += Time.unscaledDeltaTime;
            baseSource.volume = Mathf.Lerp(0f, baseVolume, t / fadeDuration);
            yield return null;
        }

        baseSource.volume = baseVolume;
    }
}