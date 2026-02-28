using UnityEngine;
using Gameplay.Player;
using Gameplay.VFX;

namespace Gameplay.Player {
    public class PlayerDamageGate : MonoBehaviour {

        [Header("Refs")]
        public PlayerHealth playerHealth;
        public HolyLightFX holyLightFX;

        [Header("Before light dies")]
        public bool preventDeathBeforeLightDies = true;

        void Awake() {
            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();

            ApplyGate();
        }

        void OnEnable() {
            if (holyLightFX != null)
                holyLightFX.OnDead += HandleLightDead;

            ApplyGate();
        }

        void OnDisable() {
            if (holyLightFX != null)
                holyLightFX.OnDead -= HandleLightDead;
        }

        // ✅ 你在“玩家被打/碰到敌人”那里调用这个
        public void TakeHit() {
            if (holyLightFX != null && !holyLightFX.IsDead)
                holyLightFX.PlayHitFX();

            ApplyGate();
        }

        void HandleLightDead() {
            ApplyGate(); // light dead -> allow death
        }

        void ApplyGate() {
            if (playerHealth == null)
                return;

            if (!preventDeathBeforeLightDies) {
                playerHealth.allowDeath = true;
                return;
            }

            bool lightDead = (holyLightFX != null && holyLightFX.IsDead);
            playerHealth.allowDeath = lightDead;
        }
    }
}