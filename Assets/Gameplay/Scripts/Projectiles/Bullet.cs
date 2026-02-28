using UnityEngine;
using Gameplay.Pooling;
using Gameplay.Units;

namespace Gameplay.Projectiles {
    public class Bullet : MonoBehaviour, IPoolable {
        public float speed = 18f;
        public float lifeTime = 1.2f;
        public int damage = 1;

        Vector2 _dir;
        float _t;
        PoolService _pool;

        public void Setup(PoolService pool) => _pool = pool;

        public void Fire(Vector2 dir) {
            _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            _t = 0f;
        }

        public void OnSpawn() {
            _t = 0f;
        }

        public void OnDespawn() {
            // nothing for now
        }

        void Update() {
            transform.position += (Vector3)(_dir * (speed * Time.deltaTime));
            _t += Time.deltaTime;

            if (_t >= lifeTime)
                _pool.Despawn(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other) {
            var d = other.GetComponent<IDamageable>();
            if (d == null)
                return;

            d.TakeDamage(damage);
            _pool.Despawn(gameObject);
        }
    }
}