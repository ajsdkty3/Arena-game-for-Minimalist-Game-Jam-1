using UnityEngine;
using Gameplay.Arena;

namespace Gameplay.Units.Movement {
    public class InertiaChordMovement : MonoBehaviour, IEnemyMovement {

        [Header("Move")]
        public float maxSpeed = 4f;
        public float acceleration = 12f;

        [Header("Arena (auto by tag)")]
        public string arenaTag = "Arena";
        public float padding = 0.2f;

        [Header("Enter Arena First")]
        public float enterMargin = 0.05f;

        [Header("Chord Target Switching")]
        public float reachDistance = 0.35f;
        public float maxTargetTime = 2.2f;

        [Header("Visual Rotation")]
        public Transform enemyImage;
        public float angleOffsetDeg = 0f;

        enum Mode { MoveIn, Chord }
        Mode _mode;

        Vector2 _vel;

        ArenaController _arena;
        bool _hasTarget;
        Vector2 _target;
        float _targetT;

        Quaternion _imageBaseLocalRot;
        bool _cached;

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

        bool IsValidArena(ArenaController a) =>
            a != null && a.gameObject.activeInHierarchy;

        void FindArena() {
            var go = GameObject.FindGameObjectWithTag(arenaTag);
            _arena = go != null ? go.GetComponent<ArenaController>() : null;
        }

        Vector2 CenterW() => _arena.center;
        float InnerR() => Mathf.Max(0f, _arena.radius - padding);

        bool IsInside(Vector2 pos) {
            float d = (pos - CenterW()).magnitude;
            return d <= InnerR() + enterMargin;
        }

        Vector2 RadialBoundaryPoint(Vector2 pos) {
            Vector2 c = CenterW();
            Vector2 v = pos - c;
            float d = v.magnitude;
            if (d <= 0.0001f)
                return c + Vector2.right * InnerR();
            return c + v / d * InnerR();
        }

        Vector2 PickRandomPointOnCircle() {
            float ang = Random.value * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            return CenterW() + dir * InnerR();
        }

        void SetTarget(Vector2 p, Transform self) {
            _target = p;
            _hasTarget = true;
            _targetT = 0f;

            // ✅ 方案 A：立即转向，但保留当前速度大小
            float speed = _vel.magnitude;
            Vector2 to = _target - (Vector2)self.position;
            if (speed > 0.0001f && to.sqrMagnitude > 0.0001f) {
                _vel = to.normalized * speed;
            }
        }

        void EnsureModeAndTarget(Transform self) {
            Vector2 pos = self.position;

            if (!IsInside(pos)) {
                _mode = Mode.MoveIn;
                SetTarget(RadialBoundaryPoint(pos), self);
                return;
            }

            if (_mode != Mode.Chord) {
                _mode = Mode.Chord;
                _hasTarget = false;
            }

            if (!_hasTarget)
                SetTarget(PickRandomPointOnCircle(), self);
        }

        public void ResetState() {
            _vel = Vector2.zero;
            _mode = Mode.MoveIn;
            _hasTarget = false;
            _targetT = 0f;

            if (!_cached)
                CacheBaseRot();

            if (enemyImage != null && _cached)
                enemyImage.localRotation = _imageBaseLocalRot;
        }

        public void Tick(Transform self, Transform unused, float dt) {
            if (!IsValidArena(_arena))
                FindArena();

            if (!IsValidArena(_arena))
                return;

            EnsureModeAndTarget(self);

            Vector2 pos = self.position;
            Vector2 to = _target - pos;
            float dist = to.magnitude;

            if (_mode == Mode.Chord) {
                _targetT += dt;
                if (dist <= reachDistance || _targetT >= maxTargetTime) {
                    SetTarget(PickRandomPointOnCircle(), self);
                    to = _target - (Vector2)self.position;
                    dist = to.magnitude;
                }
            }

            if (dist <= 0.0001f)
                return;

            // 视觉旋转
            if (enemyImage != null) {
                if (!_cached)
                    CacheBaseRot();

                float ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg + angleOffsetDeg;
                enemyImage.localRotation =
                    _imageBaseLocalRot * Quaternion.Euler(0f, 0f, ang);
            }

            // 惯性移动
            Vector2 desired = to.normalized * maxSpeed;
            _vel = Vector2.MoveTowards(_vel, desired, acceleration * dt);
            self.position += (Vector3)(_vel * dt);

            if (_mode == Mode.Chord) {
                Vector2 clamped = _arena.ClampInside(self.position, padding);
                self.position = clamped;
            }
        }
    }
}