using UnityEngine;

namespace Gameplay.Units.Movement {
    public class InertiaSeekMovement : MonoBehaviour, IEnemyMovement {

        public float maxSpeed = 4f;
        public float acceleration = 12f;

        [Header("Target")]
        public string playerTag = "Player";

        [Header("Visual Rotation")]
        public Transform enemyImage;
        public float angleOffsetDeg = 0f;

        Vector2 _vel;
        Transform _player;

        Quaternion _imageBaseLocalRot;
        bool _cached;

        void Awake() {
            CacheBaseRot();
            FindPlayer();
        }

        void OnValidate() {
            if (!Application.isPlaying)
                CacheBaseRot();
        }

        void CacheBaseRot() {
            if (enemyImage == null)
                return;

            _imageBaseLocalRot = enemyImage.localRotation;
            _cached = true;
        }

        void FindPlayer() {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            _player = go != null ? go.transform : null;
        }

        bool IsValidTarget(Transform t) {
            return t != null && t.gameObject.activeInHierarchy;
        }

        public void ResetState() {
            _vel = Vector2.zero;

            if (!_cached)
                CacheBaseRot();

            if (enemyImage != null && _cached)
                enemyImage.localRotation = _imageBaseLocalRot;
        }

        public void Tick(Transform self, Transform unused, float dt) {
            if (!IsValidTarget(_player))
                FindPlayer();

            if (!IsValidTarget(_player))
                return;

            Vector2 to = (Vector2)(_player.position - self.position);
            if (to.sqrMagnitude < 0.0001f)
                return;

            // 🔄 视觉旋转
            if (enemyImage != null) {
                if (!_cached)
                    CacheBaseRot();

                float ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg + angleOffsetDeg;
                Quaternion add = Quaternion.Euler(0f, 0f, ang);
                enemyImage.localRotation = _imageBaseLocalRot * add;
            }

            // 🧠 惯性移动
            Vector2 desired = to.normalized * maxSpeed;
            _vel = Vector2.MoveTowards(_vel, desired, acceleration * dt);
            self.position += (Vector3)(_vel * dt);
        }
    }
}