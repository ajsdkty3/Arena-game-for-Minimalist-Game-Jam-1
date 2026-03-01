using UnityEngine;

namespace Gameplay.Units.Movement {
    public class InertiaSeekAccelOverTimeMovement : MonoBehaviour, IEnemyMovement {

        [Header("Speed")]
        public float maxSpeed = 4f;

        [Header("Acceleration Over Time")]
        public float baseAcceleration = 6f;   // 初始加速度
        public float accelRamp = 4f;          // 每秒加速度增加量
        public float maxAcceleration = 25f;   // 加速度上限

        [Header("Target")]
        public string playerTag = "Player";

        [Header("Visual Rotation")]
        public Transform enemyImage;
        public float angleOffsetDeg = 0f;

        Vector2 _vel;
        Transform _player;

        Quaternion _imageBaseLocalRot;
        bool _cached;

        float _chaseTime;

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
            _chaseTime = 0f;

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
            float toSqr = to.sqrMagnitude;
            if (toSqr < 0.0001f)
                return;

            Vector2 dir = to / Mathf.Sqrt(toSqr);

            // 🔄 视觉旋转（朝向玩家）
            if (enemyImage != null) {
                if (!_cached)
                    CacheBaseRot();

                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
                enemyImage.localRotation = _imageBaseLocalRot * Quaternion.Euler(0f, 0f, ang);
            }

            // ⏳ 追击时间累计（用于 a(t)）
            _chaseTime += dt;

            // 🚀 a(t) 随时间增加
            float accel = baseAcceleration + accelRamp * _chaseTime;
            if (accel > maxAcceleration)
                accel = maxAcceleration;

            // ✅ 速度随加速度增加（积分）
            _vel += dir * (accel * dt);

            // ✅ 限速
            float spd = _vel.magnitude;
            if (spd > maxSpeed)
                _vel = _vel / spd * maxSpeed;

            // ✅ 位移随速度增加（积分）
            self.position += (Vector3)(_vel * dt);
        }
    }
}