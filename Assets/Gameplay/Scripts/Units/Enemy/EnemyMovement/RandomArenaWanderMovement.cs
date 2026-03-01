using UnityEngine;
using Gameplay.Arena;

namespace Gameplay.Units.Movement {
    public class RandomArenaWanderMovement : MonoBehaviour, IEnemyMovement {

        [Header("Arena (auto)")]
        public ArenaController arena;
        public string arenaTag = "Arena";
        public float padding = 0.2f;

        [Header("Move")]
        public float maxSpeed = 3f;
        public float acceleration = 8f;
        public float changeDirIntervalMin = 1f;
        public float changeDirIntervalMax = 2.5f;

        [Header("Visual Rotation")]
        public Transform enemyImage;
        public float angleOffsetDeg = 0f;

        enum Mode { MoveIn, Wander }
        Mode _mode;

        Vector2 _vel;
        Vector2 _targetDir;
        float _dirTimer, _dirDuration;

        Quaternion _imageBaseLocalRot;
        bool _cached;

        public bool IsActive => _mode == Mode.Wander;

        void Awake() {
            CacheBaseRot();
            ResolveArena();
            ResetState();
        }

        void OnEnable() {
            ResolveArena();
        }

        void OnValidate() {
            if (!Application.isPlaying)
                CacheBaseRot();
        }

        void ResolveArena() {
            if (arena != null)
                return;

            if (!string.IsNullOrEmpty(arenaTag)) {
                var go = GameObject.FindGameObjectWithTag(arenaTag);
                if (go != null)
                    arena = go.GetComponent<ArenaController>();
            }

            if (arena == null)
                arena = Object.FindFirstObjectByType<ArenaController>();
        }

        void CacheBaseRot() {
            if (enemyImage == null)
                return;
            _imageBaseLocalRot = enemyImage.localRotation;
            _cached = true;
        }

        public void ResetState() {
            _vel = Vector2.zero;
            _mode = Mode.MoveIn;
            PickNewDirection();

            if (!_cached)
                CacheBaseRot();
            if (enemyImage != null && _cached)
                enemyImage.localRotation = _imageBaseLocalRot;
        }

        void PickNewDirection() {
            _targetDir = Random.insideUnitCircle.normalized;
            _dirDuration = Random.Range(changeDirIntervalMin, changeDirIntervalMax);
            _dirTimer = 0f;
        }

        bool IsInsideArena(Vector2 pos) {
            var v = pos - arena.center;
            float maxR = Mathf.Max(0f, arena.radius - padding);
            return v.sqrMagnitude <= maxR * maxR;
        }

        Vector2 DirectionToEnter(Vector2 pos) {
            Vector2 clamped = arena.ClampInside(pos, padding);
            Vector2 to = clamped - pos;

            if (to.sqrMagnitude < 0.0001f) {
                to = arena.center - pos;
                if (to.sqrMagnitude < 0.0001f)
                    return Vector2.zero;
            }

            return to.normalized;
        }

        public void Tick(Transform self, Transform unused, float dt) {
            if (arena == null) {
                ResolveArena();
                if (arena == null)
                    return;
            }

            Vector2 pos = self.position;

            // 外面：进场
            if (_mode == Mode.MoveIn) {
                if (IsInsideArena(pos)) {
                    _mode = Mode.Wander;
                    PickNewDirection();
                    // 不 return：这一帧继续往下执行 wander，避免“卡一下”
                } else {
                    Vector2 dirIn = DirectionToEnter(pos);
                    if (dirIn == Vector2.zero)
                        return;

                    Vector2 desiredIn = dirIn * maxSpeed;
                    _vel = Vector2.MoveTowards(_vel, desiredIn, acceleration * dt);

                    self.position = pos + _vel * dt;
                    RotateByVelocity();
                    return;
                }
            }

            // 里面：随机游荡
            _dirTimer += dt;
            if (_dirTimer >= _dirDuration)
                PickNewDirection();

            Vector2 desired = _targetDir * maxSpeed;
            _vel = Vector2.MoveTowards(_vel, desired, acceleration * dt);

            Vector2 nextPos = (Vector2)self.position + _vel * dt;
            nextPos = arena.ClampInside(nextPos, padding);
            self.position = nextPos;

            RotateByVelocity();
        }

        void RotateByVelocity() {
            if (enemyImage == null)
                return;
            if (_vel.sqrMagnitude <= 0.001f)
                return;

            if (!_cached)
                CacheBaseRot();

            float ang = Mathf.Atan2(_vel.y, _vel.x) * Mathf.Rad2Deg + angleOffsetDeg;
            enemyImage.localRotation = _imageBaseLocalRot * Quaternion.Euler(0f, 0f, ang);
        }
    }
}