using Gameplay.Player;
using Gameplay.Pooling;
using Gameplay.Units.Movement;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Units {
    public class Enemy : MonoBehaviour, IDamageable, IPoolable {

        Transform _target;
        IEnemyMovement _movement;
        PoolService _pool;

        public int maxHp = 3;
        int _hp;

        public bool IsDamaged => _hp < maxHp;

        [Header("Collision")]
        public float hitDisableCooldown = 0.05f;
        float _hitCd;

        [Header("Hit React (visual child)")]
        public EnemyHitReact hitReact;

        [Header("Hit Stop")]
        public float hitStopTime = 0.04f;
        float _hitStop;

        // ✅ 新增：死亡音效
        [Header("Audio")]
        public AudioClip deathClip;
        [Range(0f, 1f)] public float deathVolume = 1f;
        public bool randomDeathPitch = true;
        public float deathPitchMin = 0.9f;
        public float deathPitchMax = 1.1f;

        void Awake() {
            _movement = GetComponent<IEnemyMovement>();

            if (hitReact == null)
                hitReact = GetComponentInChildren<EnemyHitReact>(true);
        }

        public void Setup(PoolService pool) => _pool = pool;

        public void Init(Transform target) {
            _target = target;
            _hp = maxHp;
            _hitCd = 0f;
            _hitStop = 0f;

            _movement?.ResetState();
        }

        public void TakeDamage(int dmg) {
            _hp -= dmg;

            if (hitReact != null)
                hitReact.Play();

            _hitStop = hitStopTime;

            if (_hp <= 0)
                Die();
        }

        void Die() {
            PlayDeathAudio();
            DespawnOrDisable();
        }

        void PlayDeathAudio() {
            if (deathClip == null)
                return;

            if (SfxOneShotHub2D.I == null)
                return;

            float pitch = 1f;
            if (randomDeathPitch)
                pitch = Random.Range(deathPitchMin, deathPitchMax);

            SfxOneShotHub2D.I.Play(deathClip, deathVolume, pitch, $"DEATH/{name}");
        }

        public void OnSpawn() { }

        public void OnDespawn() {
            _target = null;
        }

        void Update() {
            if (_hitCd > 0f)
                _hitCd -= Time.deltaTime;

            if (_hitStop > 0f) {
                _hitStop -= Time.deltaTime;
                return;
            }

            if (_movement == null || _target == null)
                return;

            _movement.Tick(transform, _target, Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other) {
            if (_hitCd > 0f)
                return;

            if (!other.CompareTag("Player"))
                return;

            var gate = other.GetComponent<PlayerDamageGate>();
            if (gate != null)
                gate.TakeHit();

            var hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
                hp.Die();

            _hitCd = hitDisableCooldown;

            Die(); // ✅ 撞到玩家也播死亡音效
        }

        void DespawnOrDisable() {
            if (_pool != null)
                _pool.Despawn(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}