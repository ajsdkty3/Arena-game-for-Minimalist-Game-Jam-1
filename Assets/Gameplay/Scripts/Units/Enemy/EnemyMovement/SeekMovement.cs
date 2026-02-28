using UnityEngine;

namespace Gameplay.Units.Movement {
    public class SeekMovement : MonoBehaviour, IEnemyMovement {
        public float moveSpeed = 3f;

        public void ResetState() { }

        public void Tick(Transform self, Transform target, float dt) {
            Vector2 dir = target.position - self.position;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            // ✅ 始终朝向玩家（正方向 = 右）
            self.right = dir.normalized;

            // 移动
            self.position += (Vector3)(dir.normalized * moveSpeed * dt);
        }
    }
}