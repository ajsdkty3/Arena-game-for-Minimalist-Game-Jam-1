using Gameplay.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Player {
    public class PlayerHealth : MonoBehaviour {
        bool _dead;
        public Gameplay.Core.SurvivalTimer timer;

        public bool allowDeath = true;

        public void Die() {
            if (_dead)
                return;

            if (!allowDeath)
                return;

            _dead = true;

            // ✅ 先抖一下（时间很短）
            if (CameraShake2D.I != null)
                CameraShake2D.I.Shake(0.12f, 0.15f);

            if (timer != null)
                timer.CommitRun();

            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}