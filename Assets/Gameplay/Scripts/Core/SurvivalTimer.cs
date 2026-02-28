using UnityEngine;

namespace Gameplay.Core {
    public class SurvivalTimer : MonoBehaviour {
        const string BestKey = "BEST_SURVIVAL_SEC";

        float _start;
        bool _running;

        public void StartRun() {
            _start = Time.time;
            _running = true;
        }

        public float CurrentSeconds() {
            if (!_running)
                return 0f;
            return Mathf.Max(0f, Time.time - _start);
        }

        public static float BestSeconds()
            => PlayerPrefs.GetFloat(BestKey, 0f);

        public void CommitRun() {
            float t = CurrentSeconds();
            float best = BestSeconds();

            if (t > best) {
                PlayerPrefs.SetFloat(BestKey, t);
                PlayerPrefs.Save();
            }

            _running = false;
        }

        public void ResetBest() {
            PlayerPrefs.SetFloat(BestKey, 0f);
            PlayerPrefs.Save();

            Debug.Log("[Timer] Best survival time reset to 0.");
        }
    }
}