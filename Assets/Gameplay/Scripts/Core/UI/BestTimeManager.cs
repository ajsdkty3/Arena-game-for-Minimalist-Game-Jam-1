using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Core {
    public class BestTimeManager : MonoBehaviour {
        const string BestTimeKey = "BestTime";

        float _startTime;
        bool _running;

        public static float CurrentTime { get; private set; }
        public static float BestTime { get; private set; }

        void Awake() {
            BestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        }

        void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            StartTimer();
        }

        void StartTimer() {
            _startTime = Time.time;
            _running = true;
        }

        void Update() {
            if (!_running)
                return;

            CurrentTime = Time.time - _startTime;
        }

        public void StopAndCheckRecord() {
            if (!_running)
                return;

            _running = false;

            float finalTime = CurrentTime;

            if (finalTime > BestTime) {
                BestTime = finalTime;
                PlayerPrefs.SetFloat(BestTimeKey, BestTime);
                PlayerPrefs.Save();
            }
        }
    }
}