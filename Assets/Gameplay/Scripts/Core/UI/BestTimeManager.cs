using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Core {

    public class BestTimeManager : MonoBehaviour {

        [Header("Persistence")]
        [Tooltip("Unique id per mode. Example: Cloud / Rogue. This becomes part of PlayerPrefs key.")]
        public string modeId = "Cloud";

        [Header("Auto Start")]
        [Tooltip("If true, timer auto-starts when entering gameplay scene(s).")]
        public bool autoStartInGameplayScenes = true;

        [Tooltip("Scenes that should auto-start the timer. Put your actual gameplay scene names here.")]
        public string[] gameplaySceneNames = { "Game" };

        // =========================
        // Runtime
        // =========================
        float _startTime;
        bool _running;

        public static BestTimeManager Instance { get; private set; }

        public static float CurrentTime { get; private set; }
        public static float BestTime { get; private set; }

        string Key => $"BestTime_{modeId}";

        void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadBest();
        }

        void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            // ✅ 修复：不是任何 sceneLoaded 都 StartTimer
            // 只在你指定的 gameplay scenes 才自动开始
            if (!autoStartInGameplayScenes)
                return;

            if (IsGameplayScene(scene.name)) {
                BeginRun(resetCurrent: true);
            } else {
                // 离开 gameplay 场景时，默认停止计时（不结算纪录）
                StopTimer(keepCurrent: true);
            }
        }

        void Update() {
            if (!_running)
                return;

            CurrentTime = Time.time - _startTime;
        }

        bool IsGameplayScene(string sceneName) {
            if (gameplaySceneNames == null || gameplaySceneNames.Length == 0)
                return false;

            for (int i = 0; i < gameplaySceneNames.Length; i++) {
                if (sceneName == gameplaySceneNames[i])
                    return true;
            }
            return false;
        }

        void LoadBest() {
            BestTime = PlayerPrefs.GetFloat(Key, 0f);
        }

        void SaveBest() {
            PlayerPrefs.SetFloat(Key, BestTime);
            PlayerPrefs.Save();
        }

        // =========================
        // Public API
        // =========================

        /// <summary>Call when the run starts (or auto-start will call it).</summary>
        public void BeginRun(bool resetCurrent) {
            if (resetCurrent)
                CurrentTime = 0f;

            _startTime = Time.time;
            _running = true;
        }

        /// <summary>Stops timer without checking record (e.g. leaving gameplay).</summary>
        public void StopTimer(bool keepCurrent) {
            _running = false;
            if (!keepCurrent)
                CurrentTime = 0f;
        }

        /// <summary>Call when the run ends (death / game over). This checks and saves best time.</summary>
        public void EndRunAndCheckRecord() {
            if (!_running)
                return;

            _running = false;

            float finalTime = CurrentTime;

            if (finalTime > BestTime) {
                BestTime = finalTime;
                SaveBest();
            }
        }

        /// <summary>Optional: reset best time for this mode.</summary>
        public void ResetBest() {
            BestTime = 0f;
            SaveBest();
        }

        /// <summary>Switch mode at runtime (Cloud/Rogue), then reload best from prefs.</summary>
        public void SetMode(string newModeId) {
            if (string.IsNullOrEmpty(newModeId))
                return;

            modeId = newModeId;
            LoadBest();
        }
    }
}