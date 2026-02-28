using Gameplay.Arena;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Player {
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour {
        [Header("Refs")]
        public ArenaController arena;
        public InputActionReference move; // Gameplay/Move (Vector2)

        [Header("Tuning")]
        public float moveSpeed = 6f;
        public float clampPadding = 0.5f;

        Rigidbody2D _rb;
        Vector2 _move;

        void Awake() {
            _rb = GetComponent<Rigidbody2D>();
        }

        void OnEnable() {
            move?.action.Enable();
        }

        void OnDisable() {
            move?.action.Disable();
        }

        void Update() {
            _move = move != null
                ? move.action.ReadValue<Vector2>()
                : Vector2.zero;
        }

        void FixedUpdate() {
            var pos = _rb.position + _move * (moveSpeed * Time.fixedDeltaTime);

            if (arena != null)
                pos = arena.ClampInside(pos, clampPadding);

            _rb.MovePosition(pos);
        }
    }
}