using UnityEngine;
using Gameplay.VFX;

namespace Gameplay.Player {
    public class PlayerDamageGate : MonoBehaviour {

        [Header("Refs")]
        public PlayerHealth playerHealth;
        public HolyLightFX holyLightFX;

        void Awake() {
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();
        }

        // ✅ Enemy 碰到玩家时调用这个
        public void TakeHit(int damage) {
            // holy light 只是反馈，不决定生死
            if (holyLightFX != null)
                holyLightFX.PlayHitFX();

            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }
    }
}