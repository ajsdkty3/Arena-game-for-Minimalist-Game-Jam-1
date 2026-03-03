using Gameplay.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Player {

    public class PlayerHealth : MonoBehaviour {

        [Header("HP")]
        public int maxHp = 5;
        int _hp;

        public bool allowDeath = true;

        // =========================
        // HIT AUDIO
        // =========================
        [Header("Hit Audio")]
        public AudioSource hitAudioSource;
        public AudioClip[] hitClips;
        [Range(0f, 1f)] public float hitVolume = 1f;
        public float hitSoundCooldown = 0.05f;

        float _hitSoundTimer;

        // =========================
        // DEATH AUDIO
        // =========================
        [Header("Death Audio")]
        public AudioSource deathAudioSource;
        public AudioClip deathClip;
        [Range(0f, 1f)] public float deathVolume = 1f;

        public bool muteAllOtherAudioOnDeath = true;

        // =========================
        // DEATH VISUAL
        // =========================
        [Header("Death Visual")]
        public Renderer playerImageRenderer;

        public string color1Property = "_Color_1";
        public string color2Property = "_Color_2";

        public Color targetColor1 = new Color32(93, 0, 0, 255); // 5D0000
        public Color targetColor2 = Color.black;

        MaterialPropertyBlock _mpb;
        Color _startColor1;
        Color _startColor2;

        bool _dying;

        // =========================
        // EXTRA DEATH VISUAL
        // =========================
        [Header("Extra Death Visual")]
        public UnityEngine.Rendering.Universal.Light2D playerLight;
        public SpriteRenderer playerShadow;

        float _startLightIntensity;
        Color _startShadowColor;

        public string reloadScene = "Menu";
        void Awake() {
            _hp = maxHp;

            if (playerImageRenderer != null)
                _mpb = new MaterialPropertyBlock();
        }

        void Update() {
            if (_hitSoundTimer > 0f)
                _hitSoundTimer -= Time.unscaledDeltaTime;
        }

        public void TakeDamage(int amount) {
            if (_hp <= 0 || _dying)
                return;

            _hp -= amount;

            CameraShake2D.I?.Shake(0.1f, 0.2f);

            PlayHitSound();

            if (_hp <= 0 && allowDeath)
                Die();
        }

        void PlayHitSound() {
            if (hitAudioSource == null)
                return;

            if (hitClips == null || hitClips.Length == 0)
                return;

            if (_hitSoundTimer > 0f)
                return;

            _hitSoundTimer = hitSoundCooldown;

            int index = Random.Range(0, hitClips.Length);
            hitAudioSource.PlayOneShot(hitClips[index], hitVolume);
        }

        public void Die() {
            if (_dying)
                return;

            _dying = true;

            // ✅ 起始颜色直接固定（Color1=白，Color2=#FFD900）
            CaptureCurrentMaterialColors();

            StartCoroutine(DeathRoutine());
        }

        void CaptureCurrentMaterialColors() {

            // ✅ 关键改动：不再从 MPB/Material 读取，直接用预设起始值
            _startColor1 = Color.white;
            _startColor2 = new Color32(255, 217, 0, 255); // FFD900

            // capture light
            if (playerLight != null)
                _startLightIntensity = playerLight.intensity;

            // capture shadow
            if (playerShadow != null)
                _startShadowColor = playerShadow.color;
        }

        IEnumerator DeathRoutine() {
            FindObjectOfType<BestTimeManager>().StopAndCheckRecord();
            CameraShake2D.I?.Shake(0.2f, 0.35f);

            Time.timeScale = 0f;

            float waitSec = 0f;

            if (deathClip != null && deathAudioSource != null) {

                deathAudioSource.ignoreListenerPause = true;

                if (muteAllOtherAudioOnDeath)
                    AudioListener.pause = true;

                deathAudioSource.Stop();
                deathAudioSource.clip = deathClip;
                deathAudioSource.volume = deathVolume;
                deathAudioSource.Play();

                float pitch = Mathf.Max(0.01f, deathAudioSource.pitch);
                waitSec = deathClip.length / pitch;
            }

            if (waitSec <= 0f)
                waitSec = 1f;

            float t = 0f;

            while (t < waitSec) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / waitSec);

                UpdateDeathVisual(k);

                yield return null;
            }

            SceneManager.LoadScene(reloadScene);
        }

        void UpdateDeathVisual(float k) {

            if (playerImageRenderer == null || _mpb == null)
                return;

            Color c1 = Color.Lerp(_startColor1, targetColor1, k);
            Color c2 = Color.Lerp(_startColor2, targetColor2, k);

            playerImageRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(color1Property, c1);
            _mpb.SetColor(color2Property, c2);
            playerImageRenderer.SetPropertyBlock(_mpb);

            if (playerLight != null)
                playerLight.intensity = Mathf.Lerp(_startLightIntensity, 0f, k);

            // fade shadow to black
            if (playerShadow != null)
                playerShadow.color = Color.Lerp(_startShadowColor, Color.black, k);
        }
    }
}