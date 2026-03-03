using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmManager : MonoBehaviour {
    public static BgmManager Instance;

    [System.Serializable]
    public class SceneBgm {
        public string sceneName;
        public AudioClip clip;
        public bool stopIfClipIsNull = true;
    }

    [Header("Scene BGM Mapping")]
    public List<SceneBgm> sceneBgms = new List<SceneBgm>();

    [Header("Behavior")]
    public bool stopIfNoClip = true;

    [Header("Fade")]
    public float fadeDuration = 1f;
    [Range(0f, 1f)] public float volume = 1f;

    AudioSource _audio;
    Coroutine _fadeRoutine;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        _audio.loop = true;
        _audio.playOnAwake = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start() {
        PlayBgmForScene(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        PlayBgmForScene(scene.name);
    }

    void PlayBgmForScene(string sceneName) {
        SceneBgm sceneData = null;

        foreach (var entry in sceneBgms) {
            if (string.Equals(entry.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase)) {
                sceneData = entry;
                break;
            }
        }

        if (sceneData == null) {
            if (stopIfNoClip)
                StartFadeTo(null);
            return;
        }

        var targetClip = sceneData.clip;

        if (targetClip == null) {
            if (sceneData.stopIfClipIsNull)
                StartFadeTo(null);
            return;
        }

        if (_audio.clip == targetClip) {
            if (!_audio.isPlaying)
                ForcePlayCurrent();
            return;
        }

        StartFadeTo(targetClip);
    }

    void StartFadeTo(AudioClip newClip) {
        if (_fadeRoutine != null) {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _fadeRoutine = StartCoroutine(FadeTo(newClip));
    }

    void ForcePlayCurrent() {
        _audio.mute = false;
        _audio.volume = Mathf.Clamp01(volume);
        _audio.loop = true;
        _audio.Play();
    }

    IEnumerator FadeTo(AudioClip newClip) {

        if (_audio.isPlaying && fadeDuration > 0.0001f) {
            float t = 0f;
            float startVol = _audio.volume;

            while (t < fadeDuration) {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / fadeDuration);
                _audio.volume = Mathf.Lerp(startVol, 0f, a);
                yield return null;
            }
        }

        _audio.Stop();
        _audio.clip = newClip;

        if (newClip == null) {
            _audio.volume = Mathf.Clamp01(volume);
            _fadeRoutine = null;
            yield break;
        }

        _audio.mute = false;
        _audio.loop = true;
        _audio.volume = 0f;
        _audio.Play();

        if (fadeDuration <= 0.0001f) {
            _audio.volume = Mathf.Clamp01(volume);
            _fadeRoutine = null;
            yield break;
        }

        float time = 0f;
        while (time < fadeDuration) {
            time += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(time / fadeDuration);
            _audio.volume = Mathf.Lerp(0f, Mathf.Clamp01(volume), a);
            yield return null;
        }

        _audio.volume = Mathf.Clamp01(volume);
        _fadeRoutine = null;
    }
}