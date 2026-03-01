using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Player {

    public class PlayerHealth : MonoBehaviour {

        [Header("HP")]
        public int maxHp = 5;
        int _hp;

        public bool allowDeath = true;

        [Header("Hit Audio")]
        public AudioSource audioSource;   // 外部拖引用
        public AudioClip[] hitClips;
        [Range(0f, 1f)] public float hitVolume = 1f;
        public float hitSoundCooldown = 0.05f;

        float _hitSoundTimer;

        void Awake() {
            _hp = maxHp;
        }

        void Update() {
            if (_hitSoundTimer > 0f)
                _hitSoundTimer -= Time.unscaledDeltaTime;
        }

        public void TakeDamage(int amount) {
            if (_hp <= 0)
                return;

            _hp -= amount;

            CameraShake2D.I?.Shake(0.1f, 0.2f);

            PlayHitSound();

            Debug.Log("HP:" + _hp);

            if (_hp <= 0 && allowDeath) {
                Die();
            }
        }

        void PlayHitSound() {
            if (audioSource == null)
                return;

            if (hitClips == null || hitClips.Length == 0)
                return;

            if (_hitSoundTimer > 0f)
                return;

            _hitSoundTimer = hitSoundCooldown;

            int index = Random.Range(0, hitClips.Length);
            audioSource.PlayOneShot(hitClips[index], hitVolume);
        }

        public void Die() {
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine() {

            CameraShake2D.I?.Shake(0.2f, 0.35f);

            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(1f);

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}