using UnityEngine;

public class HolyLightDead_BgmSwitch : MonoBehaviour {

    public Gameplay.VFX.HolyLightFX holyLightFX;

    [Header("BGM Switch")]
    public AudioClip deadBaseTrack;          // 光黑后的 base
    public bool keepLayerPlaying = true;     // 是否保留 layer
    public bool restartLayer = false;        // 需要的话也可以重播 layer

    void Awake() {
        if (holyLightFX == null)
            holyLightFX = FindFirstObjectByType<Gameplay.VFX.HolyLightFX>();
    }

    void OnEnable() {
        if (holyLightFX != null)
            holyLightFX.OnDead += HandleDead;
    }

    void OnDisable() {
        if (holyLightFX != null)
            holyLightFX.OnDead -= HandleDead;
    }

    void HandleDead() {
        if (BgmPlayer.Instance != null && deadBaseTrack != null) {
            BgmPlayer.Instance.SwitchBaseTrack(deadBaseTrack);
        }

        if (!keepLayerPlaying && BgmPlayer.Instance != null && BgmPlayer.Instance.layerSource != null) {
            BgmPlayer.Instance.layerSource.Stop();
        } else if (restartLayer && BgmPlayer.Instance != null && BgmPlayer.Instance.layerSource != null) {
            // 可选：重播 layer（比如想让 layer 跟着从头开始）
            var src = BgmPlayer.Instance.layerSource;
            src.time = 0f;
            if (!src.isPlaying)
                src.Play();
        }
    }
}