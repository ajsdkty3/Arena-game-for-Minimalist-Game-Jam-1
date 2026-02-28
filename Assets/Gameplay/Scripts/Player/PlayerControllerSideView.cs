using Gameplay.Arena;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Player {
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerControllerSideView : MonoBehaviour {
        [Header("Refs")]
        public ArenaController arena;
        public InputActionReference move;   // Gameplay/Move (Vector2) use x
        public InputActionReference jump;   // Gameplay/Jump (Button)

        [Header("Move")]
        public float moveSpeed = 6f;
        public float accel = 60f;
        public float decel = 80f;

        [Header("Jump")]
        public float jumpSpeed = 12f;
        public float coyoteTime = 0.08f;
        public float jumpBuffer = 0.10f;

        [Header("Ground Check (BoxCast)")]
        public LayerMask groundMask;                        // ✅ 只勾 Ground/Platform 等地面层
        public Vector2 groundBoxSize = new Vector2(0.6f, 0.15f); // ✅ 脚底检测盒子宽高
        public float groundBoxDistance = 0.08f;             // ✅ 往下探多远

        [Header("Ground Fix")]
        public float ignoreGroundAfterJump = 0.06f;         // 起跳后短暂忽略 grounded（只在上升时生效）
        public float groundedMaxUpVel = 0.05f;              // 上升时不算 grounded（防贴地误判）

        [Header("Arena Clamp")]
        public float clampPadding = 0.5f;

        Rigidbody2D _rb;
        Collider2D _selfCol;
        Vector2 _move;

        float _lastGroundedTime = -999f;
        float _lastJumpPressedTime = -999f;
        float _ignoreGroundUntil = -999f;

        void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            _selfCol = GetComponent<Collider2D>();
        }

        void OnEnable() {
            move?.action.Enable();
            jump?.action.Enable();
        }

        void OnDisable() {
            move?.action.Disable();
            jump?.action.Disable();
        }

        void Update() {
            _move = move != null ? move.action.ReadValue<Vector2>() : Vector2.zero;

            if (jump != null && jump.action.WasPressedThisFrame())
                _lastJumpPressedTime = Time.time;
        }

        void FixedUpdate() {
            bool grounded = IsGrounded();
            if (grounded) {
                _lastGroundedTime = Time.time;
                _ignoreGroundUntil = -999f; // ✅ 落地立刻取消忽略（避免“过一会才能跳”）
            }

            // horizontal
            float targetVX = _move.x * moveSpeed;
            float vx = _rb.linearVelocity.x;
            float rate = Mathf.Abs(targetVX) > 0.001f ? accel : decel;
            vx = Mathf.MoveTowards(vx, targetVX, rate * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);

            // jump
            bool canCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;
            bool buffered = (Time.time - _lastJumpPressedTime) <= jumpBuffer;

            if (buffered && canCoyote) {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpSpeed);

                _lastJumpPressedTime = -999f;
                _lastGroundedTime = -999f;

                _ignoreGroundUntil = Time.time + Mathf.Max(0f, ignoreGroundAfterJump);
            }

            ApplyArenaClamp();
        }

        bool IsGrounded() {
            if (_selfCol == null)
                return false;

            float vy = _rb.linearVelocity.y;

            // ✅ 起跳后短暂忽略 grounded：但一旦开始下落，就不要再忽略了
            if (Time.time < _ignoreGroundUntil && vy > 0f)
                return false;

            // ✅ 上升时不算 grounded（可选，但很有用）
            if (vy > groundedMaxUpVel)
                return false;

            // ✅ 用自己 collider 的 bounds 来做脚底 BoxCast（稳定，不容易漏）
            Bounds b = _selfCol.bounds;

            // box 起点：collider 底部稍微往上一点点，避免一开始就嵌进地面
            Vector2 origin = new Vector2(b.center.x, b.min.y + groundBoxSize.y * 0.5f);

            RaycastHit2D hit = Physics2D.BoxCast(
                origin,
                groundBoxSize,
                0f,
                Vector2.down,
                Mathf.Max(0.0001f, groundBoxDistance),
                groundMask
            );

            // 过滤自己（理论上 BoxCast 不会打到自己，但加个保险）
            if (hit.collider == null || hit.collider == _selfCol)
                return false;

            return true;
        }

        void ApplyArenaClamp() {
            if (arena == null)
                return;

            Vector2 before = _rb.position;
            Vector2 after = arena.ClampInside(before, clampPadding);

            Vector2 delta = after - before;
            if (delta.sqrMagnitude < 0.0000001f)
                return;

            _rb.MovePosition(after);

            // 碰到边界就清掉对应方向速度，防止沿边界“爬”
            Vector2 v = _rb.linearVelocity;
            if (Mathf.Abs(delta.x) > 0.00001f)
                v.x = 0f;
            if (Mathf.Abs(delta.y) > 0.00001f)
                v.y = 0f;
            _rb.linearVelocity = v;
        }

        void OnDrawGizmosSelected() {
            // 运行时可视化 BoxCast（Scene 里看得到脚底检测盒）
            if (!Application.isPlaying)
                return;

            if (_selfCol == null)
                return;

            Bounds b = _selfCol.bounds;
            Vector2 origin = new Vector2(b.center.x, b.min.y + groundBoxSize.y * 0.5f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(origin, groundBoxSize);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(origin + Vector2.down * groundBoxDistance, groundBoxSize);
        }
    }
}