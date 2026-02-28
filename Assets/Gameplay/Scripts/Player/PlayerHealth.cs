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

            if (timer != null)
                timer.CommitRun();
            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}