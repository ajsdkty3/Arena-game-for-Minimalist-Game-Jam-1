using UnityEngine;

public class SfxOneShotHub2D : MonoBehaviour {
    public static SfxOneShotHub2D I { get; private set; }

    [Header("Audio")]
    public AudioSource source;

    [Header("Debug")]
    public bool debugLogs = true;

    void Awake() {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        // ✅ 2D：完全不做空间衰减
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 0 = 2D
        source.dopplerLevel = 0f;
        source.loop = false;

        if (debugLogs) {
            Debug.Log($"[SfxHub2D] Awake. source={source != null} spatialBlend={source.spatialBlend}");
            Debug.Log($"[SfxHub2D] AudioListener.volume={AudioListener.volume} pause={AudioListener.pause}");
        }
    }

    public void Play(AudioClip clip, float volume = 1f, float pitch = 1f, string tag = "") {
        if (clip == null) {
            if (debugLogs)
                Debug.LogWarning($"[SfxHub2D] Play called but clip is NULL. tag={tag}");
            return;
        }

        if (source == null) {
            if (debugLogs)
                Debug.LogError("[SfxHub2D] source is NULL (unexpected).");
            return;
        }

        // 关键的全局状态打印
        if (debugLogs) {
            Debug.Log($"[SfxHub2D] PlayOneShot '{clip.name}' vol={volume:F2} pitch={pitch:F2} " +
                      $"listenerVol={AudioListener.volume:F2} pause={AudioListener.pause} timeScale={Time.timeScale} tag={tag}");
        }

        source.pitch = pitch;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ✅ 用来确认“Unity 音频系统本身能不能响”
    // 运行时按 T，会尝试播放你在 Inspector 里填的 testClip
    [Header("Test")]
    public AudioClip testClip;
    void Update() {
        if (testClip != null && Input.GetKeyDown(KeyCode.T)) {
            Play(testClip, 1f, 1f, "TEST_KEY_T");
        }
    }
}