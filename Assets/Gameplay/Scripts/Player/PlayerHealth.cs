using UnityEngine;

namespace Gameplay.Player {
    public class PlayerHealth : MonoBehaviour {
        public int maxHp = 5;
        int _hp;

        public bool allowDeath = true;

        void Awake() {
            _hp = maxHp;
        }

        public void TakeDamage(int amount) {
            if (_hp <= 0)
                return;

            _hp -= amount;

            // 🔥 被打立即抖
            CameraShake2D.I?.Shake(0.1f, 0.2f);

            if (_hp <= 0 && allowDeath) {
                Die();
            }
        }

        public void Die() {
            // 死亡时更强一点
            CameraShake2D.I?.Shake(0.2f, 0.35f);

            // 这里写你的死亡逻辑
            Debug.Log("Player Dead");
        }
    }
}