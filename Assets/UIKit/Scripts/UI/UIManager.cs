using UnityEngine;
using UnityEngine.InputSystem;
using UIKit.Input;
using UIKit.Model;
using UIKit.StateMachine;
using UIKit.States;
using UIKit.UI.Panels;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace UIKit.UI {
    public class UIManager : MonoBehaviour {
        [Header("Input")]
        public InputRouter inputRouter;

        [Header("Views (drag 3 views only)")]
        public MenuPanelView menuView;
        public PausePanelView pauseView;
        public SettingsPanelView settingsView;
        public AudioMixer audioMixer;

        // UI Panels from views
        GameObject MenuPanel => menuView.gameObject;
        GameObject PausePanel => pauseView.gameObject;
        GameObject SettingsPanel => settingsView.gameObject;

        readonly UIStateMachine _uiSm = new();
        SettingsModel _model;

        AppDomain _domain = AppDomain.UI; // 先默认进 Menu(UI域)，Gameplay 先空

        void Awake() {
            _model = new SettingsModel(audioMixer);
            _model.ApplyAudio();      // ✅ 再补一刀（安全）
            _model.ApplyGraphics();
            settingsView.Bind(_model);

            // 注册 UI 内部状态（Menu/Pause/Settings）
            _uiSm.Register(new GameState(MenuPanel, PausePanel, SettingsPanel));
            _uiSm.Register(new MenuState(MenuPanel, PausePanel, SettingsPanel));
            _uiSm.Register(new PauseState(MenuPanel, PausePanel, SettingsPanel));
            _uiSm.Register(new SettingsState(SettingsPanel));

            // 订阅 View 事件（这些事件只表达“意图”，切状态由这里统一处理）
            menuView.StartClicked += OnMenuStart;              // 目前空
            menuView.SettingsClicked += OpenSettings;
            menuView.QuitClicked += Quit;

            pauseView.ResumeClicked += ResumeToGameplay;
            pauseView.SettingsClicked += OpenSettings;
            pauseView.QuitClicked += ReloadScene;

            settingsView.BackClicked += CloseSettings;
        }

        void Start() {
            EnterUIDomainMenu();
            settingsView.RefreshAllFromModel();

            if (inputRouter != null && inputRouter.PauseAction != null) {
                inputRouter.PauseAction.performed += OnGlobalPause;
            } else {
                Debug.LogError("[UI] Cannot subscribe PauseAction (null).");
            }

            EnterUIDomainMenu();
            settingsView.RefreshAllFromModel();
            _model.ApplyGraphics();
        }

        void OnDestroy() {
            if (inputRouter != null && inputRouter.PauseAction != null)
                inputRouter.PauseAction.performed -= OnGlobalPause;
        }

        // ========= 两大域切换 =========

        void EnterGameplayDomain() {
            _domain = AppDomain.Gameplay;

            inputRouter.SetDomainGameplay();

            // ✅ 关键：清栈并进入 GameState
            _uiSm.Switch(UIStateId.Game);
        }

        void EnterUIDomainMenu() {
            _domain = AppDomain.UI;

            inputRouter.SetDomainUI();

            // UI域：用 UI 状态机控制显示
            _uiSm.Switch(UIStateId.Menu);
        }

        void EnterUIDomainPause() {
            _domain = AppDomain.UI;

            inputRouter.SetDomainUI();
            _uiSm.Push(UIStateId.Pause);
        }

        // ========= Global ESC 处理 =========

        void OnGlobalPause(InputAction.CallbackContext ctx) {            // Global ESC 是“域级开关”
            if (_domain == AppDomain.Gameplay) {
                // 游戏域按 ESC => 进入 UI Pause
                EnterUIDomainPause();
                return;
            }

            // UI 域按 ESC：按层级退
            // Settings 打开 => Pop 回 Pause/Menu
            if (_uiSm.Is(UIStateId.Settings)) {
                CloseSettings();
                return;
            }

            // Pause 打开 => Pop，然后回 GameplayDomain（因为 Pause 是覆盖在 Gameplay 上的）
            if (_uiSm.Is(UIStateId.Pause)) {
                ClosePause();
                EnterGameplayDomain(); // 目前游戏域是空的，但域独立已成立
                return;
            }

            // Menu 状态下按 ESC：你可以选择不做事（模板常见）
        }

        // ========= UI 内部意图 =========

        void OnMenuStart() {
            EnterGameplayDomain();
            // 现在要求：Start 先不做逻辑
            // 但如果你未来要“Start 进入 GameplayDomain”，就在这里调用 EnterGameplayDomain()
        }

        void OpenSettings() {
            _uiSm.Push(UIStateId.Settings);
            settingsView.RefreshAllFromModel();
        }

        void CloseSettings() {
            if (_uiSm.Is(UIStateId.Settings))
                _uiSm.Pop();
        }

        void ClosePause() {
            if (_uiSm.Is(UIStateId.Pause))
                _uiSm.Pop();

            // PauseState 本身会把 TimeScale=0；Pop 回到 MenuState 时会 TimeScale=1
            // 但当我们从 Pause 回 GameplayDomain 时，我们会在 EnterGameplayDomain 里强制恢复 TimeScale=1
        }

        public void Quit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void ResumeToGameplay() {
            ClosePause();
            EnterGameplayDomain();
        }

        void ReloadScene() {
            Time.timeScale = 1f; // 防止卡在暂停
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
    }
}