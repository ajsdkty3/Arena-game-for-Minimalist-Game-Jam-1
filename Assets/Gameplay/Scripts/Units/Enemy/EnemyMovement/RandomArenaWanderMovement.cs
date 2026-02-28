using UnityEngine;
using Gameplay.Arena;

namespace Gameplay.Units.Movement {

    public class RandomArenaWanderMovement : MonoBehaviour, IEnemyMovement {

        [Header("Arena")]
        public float padding = 0.5f;

        [Header("Player")]
        public string playerTag = "Player";

        [Header("Move")]
        public float maxSpeed = 3f;
        public float acceleration = 8f;

        [Header("Direction Change")]
        public float changeDirMin = 0.8f;
        public float changeDirMax = 1.5f;

        [Header("Return")]
        public float returnSpeedMultiplier = 1.3f;
        public float returnAccelMultiplier = 1.8f;

        [Header("Visual Rotation (optional)")]
        public Transform enemyImage;     // ✅ 只转这个
        public float angleOffsetDeg = 0f;

        Vector2 _vel;
        Vector2 _dir;

        float _changeTimer;
        float _changeDelay;

        Transform _player;
        ArenaController arena;

        Quaternion _imageBaseLocalRot;
        bool _cached;

        void Awake() {
            arena = Object.FindFirstObjectByType<ArenaController>();
            CacheBaseRot();
            FindPlayer();
            PickNewDirection();
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

        public void ResetState() {
            if (arena == null)
                arena = Object.FindFirstObjectByType<ArenaController>();

            _vel = Vector2.zero;
            PickNewDirection();

            if (!_cached)
                CacheBaseRot();
            if (enemyImage != null && _cached)
                enemyImage.localRotation = _imageBaseLocalRot;
        }

        void FindPlayer() {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            _player = go != null ? go.transform : null;
        }

        void PickNewDirection() {
            _dir = Random.insideUnitCircle;
            if (_dir.sqrMagnitude < 0.0001f)
                _dir = Vector2.right;
            _dir.Normalize();

            _changeTimer = 0f;
            _changeDelay = Random.Range(changeDirMin, changeDirMax);
        }

        bool IsOutside(Vector2 pos) {
            if (arena == null)
                return false;

            float maxR = Mathf.Max(0f, arena.radius - padding);
            Vector2 v = pos - arena.center;
            return v.sqrMagnitude > maxR * maxR;
        }

        public void Tick(Transform self, Transform unused, float dt) {

            if (_player == null)
                FindPlayer();

            Vector2 pos = self.position;

            // 🔄 只转 image 朝向 player
            if (_player != null && enemyImage != null) {
                if (!_cached)
                    CacheBaseRot();

                Vector2 toPlayer = (Vector2)(_player.position - self.position);
                if (toPlayer.sqrMagnitude > 0.0001f) {
                    float ang = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg + angleOffsetDeg;
                    Quaternion add = Quaternion.Euler(0f, 0f, ang);
                    enemyImage.localRotation = _imageBaseLocalRot * add;
                }
            }

            bool outside = arena != null && IsOutside(pos);

            Vector2 desiredDir;
            if (outside) {
                desiredDir = ((Vector2)arena.center - pos).normalized;
            } else {
                _changeTimer += dt;
                if (_changeTimer >= _changeDelay)
                    PickNewDirection();
                desiredDir = _dir;
            }

            if (desiredDir.sqrMagnitude < 0.0001f)
                return;

            float speed = maxSpeed * (outside ? returnSpeedMultiplier : 1f);
            float accel = acceleration * (outside ? returnAccelMultiplier : 1f);

            Vector2 desiredVel = desiredDir * speed;
            _vel = Vector2.MoveTowards(_vel, desiredVel, accel * dt);

            pos += _vel * dt;

            if (arena != null && !outside)
                pos = arena.ClampInside(pos, padding);

            self.position = pos;
        }
    }
}