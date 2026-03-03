using System.Collections;
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

        // =========================
        // Audio
        // =========================
        [Header("Audio")]
        public AudioClip deathClip;
        [Range(0f, 1f)] public float deathVolume = 1f;
        public bool randomDeathPitch = true;
        public float deathPitchMin = 0.9f;
        public float deathPitchMax = 1.1f;

        // =========================
        // Death visuals & cleanup
        // =========================
        [Header("Death - Disable & Fade")]
        [Tooltip("Seconds to fade EnemyImage alpha to 0 before despawn.")]
        public float deathFadeDuration = 0.5f;

        [Tooltip("Disable these colliders immediately on death. If empty, will auto-grab all Collider2D under this enemy.")]
        public Collider2D[] collidersToDisable;

        [Tooltip("Optional particles to stop on death (drag in TrailFX etc).")]
        public ParticleSystem[] particlesToStop;

        [Tooltip("Optional trail renderers to clear on death.")]
        public TrailRenderer[] trailsToClear;

        [Tooltip("Renderer on EnemyImage (MeshRenderer/SpriteRenderer) to fade out.")]
        public Renderer enemyImageRenderer;

        // internal
        bool _dying;
        Coroutine _deathRoutine;

        MaterialPropertyBlock _mpb;
        static readonly int _ColorId = Shader.PropertyToID("_Color");
        static readonly int _BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake() {
            _movement = GetComponent<IEnemyMovement>();

            if (hitReact == null)
                hitReact = GetComponentInChildren<EnemyHitReact>(true);

            if (enemyImageRenderer == null)
                enemyImageRenderer = GetComponentInChildren<Renderer>(true);

            if (collidersToDisable == null || collidersToDisable.Length == 0)
                collidersToDisable = GetComponentsInChildren<Collider2D>(true);

            _mpb = new MaterialPropertyBlock();
        }

        public void Setup(PoolService pool) => _pool = pool;

        public void Init(Transform target) {
            _target = target;
            _hp = maxHp;
            _hitCd = 0f;
            _hitStop = 0f;

            _dying = false;
            if (_deathRoutine != null) {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            // reset collision
            if (collidersToDisable != null) {
                for (int i = 0; i < collidersToDisable.Length; i++) {
                    if (collidersToDisable[i] != null)
                        collidersToDisable[i].enabled = true;
                }
            }

            // reset particles/trails
            if (particlesToStop != null) {
                for (int i = 0; i < particlesToStop.Length; i++) {
                    if (particlesToStop[i] != null) {
                        particlesToStop[i].Clear(true);
                        particlesToStop[i].Play(true);
                    }
                }
            }

            if (trailsToClear != null) {
                for (int i = 0; i < trailsToClear.Length; i++) {
                    if (trailsToClear[i] != null) {
                        trailsToClear[i].Clear();
                        trailsToClear[i].enabled = true;
                    }
                }
            }

            // reset alpha
            SetEnemyImageAlpha(1f);

            _movement?.ResetState();
        }

        public void TakeDamage(int dmg) {
            if (_dying)
                return;

            _hp -= dmg;

            if (hitReact != null)
                hitReact.Play();

            _hitStop = hitStopTime;

            if (_hp <= 0)
                Die(true);   // 被打死 → 播音效
        }

        void Die(bool playAudio) {
            if (_dying)
                return;
            _dying = true;

            if (playAudio)
                PlayDeathAudio();

            // 1) disable colliders now
            if (collidersToDisable != null) {
                for (int i = 0; i < collidersToDisable.Length; i++) {
                    if (collidersToDisable[i] != null)
                        collidersToDisable[i].enabled = false;
                }
            }

            // 2) stop particles/trails now (optional)
            if (particlesToStop != null) {
                for (int i = 0; i < particlesToStop.Length; i++) {
                    if (particlesToStop[i] != null)
                        particlesToStop[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            if (trailsToClear != null) {
                for (int i = 0; i < trailsToClear.Length; i++) {
                    if (trailsToClear[i] != null) {
                        trailsToClear[i].Clear();
                        trailsToClear[i].enabled = false;
                    }
                }
            }

            // 3) fade out then despawn
            _deathRoutine = StartCoroutine(DeathFadeThenDespawn());
        }

        IEnumerator DeathFadeThenDespawn() {
            float t = 0f;
            float dur = Mathf.Max(0.0001f, deathFadeDuration);

            while (t < dur) {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / dur);
                SetEnemyImageAlpha(a);
                yield return null;
            }

            SetEnemyImageAlpha(0f);
            DespawnOrDisable();
        }

        void SetEnemyImageAlpha(float a) {
            if (enemyImageRenderer == null)
                return;

            // Prefer property block (doesn't instantiate material)
            enemyImageRenderer.GetPropertyBlock(_mpb);

            // Try to read an existing color first (from mpb or renderer)
            Color c = Color.white;

            // If MPB already has a color set, use that as base; otherwise fallback to renderer's material color.
            // Note: MaterialPropertyBlock doesn't have "HasProperty", so we just fallback safely.
            try {
                c = _mpb.GetColor(_BaseColorId);
                if (c == default)
                    c = _mpb.GetColor(_ColorId);
                if (c == default)
                    c = enemyImageRenderer.sharedMaterial != null ? enemyImageRenderer.sharedMaterial.color : Color.white;
            } catch {
                c = enemyImageRenderer.sharedMaterial != null ? enemyImageRenderer.sharedMaterial.color : Color.white;
            }

            c.a = a;

            var mat = enemyImageRenderer.sharedMaterial;
            if (mat != null && mat.HasProperty(_BaseColorId)) {
                _mpb.SetColor(_BaseColorId, c);
            } else if (mat != null && mat.HasProperty(_ColorId)) {
                _mpb.SetColor(_ColorId, c);
            } else {
                // last resort
                _mpb.SetColor(_ColorId, c);
            }

            enemyImageRenderer.SetPropertyBlock(_mpb);
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
            if (_dying)
                return;

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
            if (_dying)
                return;
            if (_hitCd > 0f)
                return;
            if (!other.CompareTag("Player"))
                return;

            var gate = other.GetComponent<PlayerDamageGate>();
            if (gate != null)
                gate.TakeHit(1);

            _hitCd = hitDisableCooldown;

            Die(false);   // 撞死 → 不播音效（但仍然渐隐+关碰撞+停粒子）
        }

        void DespawnOrDisable() {
            if (_pool != null)
                _pool.Despawn(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}