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

            Debug.Log("HP:" + _hp);

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

            // ✅ 关键修正：死亡瞬间读取“当前真实颜色”
            CaptureCurrentMaterialColors();

            StartCoroutine(DeathRoutine());
        }

        void CaptureCurrentMaterialColors() {

            if (playerImageRenderer == null || _mpb == null)
                return;

            playerImageRenderer.GetPropertyBlock(_mpb);

            // 先尝试从 property block 读
            if (_mpb.isEmpty) {
                // 如果 block 里没有值，从 sharedMaterial 读
                var mat = playerImageRenderer.sharedMaterial;
                if (mat != null) {
                    if (mat.HasProperty(color1Property))
                        _startColor1 = mat.GetColor(color1Property);

                    if (mat.HasProperty(color2Property))
                        _startColor2 = mat.GetColor(color2Property);
                }
            } else {
                _startColor1 = _mpb.GetColor(color1Property);
                _startColor2 = _mpb.GetColor(color2Property);
            }

            // capture light
            if (playerLight != null)
                _startLightIntensity = playerLight.intensity;

            // capture shadow
            if (playerShadow != null)
                _startShadowColor = playerShadow.color;
        }

        IEnumerator DeathRoutine() {

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

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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