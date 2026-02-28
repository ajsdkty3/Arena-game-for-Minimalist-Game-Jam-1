using Gameplay.Player;
using Gameplay.Pooling;
using Gameplay.Units.Movement;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Units {
    public class Enemy : MonoBehaviour, IDamageable, IPoolable {
        Transform _target;          // ✅ always player
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
        [Tooltip("Only stops this enemy's movement tick, not global time.")]
        public float hitStopTime = 0.04f;
        float _hitStop;

        void Awake() {
            _movement = GetComponent<IEnemyMovement>();

            if (hitReact == null)
                hitReact = GetComponentInChildren<EnemyHitReact>(true);
        }

        public void Setup(PoolService pool) => _pool = pool;

        // ✅ spawner/pool call this with the player transform
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
                DespawnOrDisable();
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

            // ✅ 撞到玩家后自己消失
            DespawnOrDisable();
        }

        void DespawnOrDisable() {
            if (_pool != null)
                _pool.Despawn(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}