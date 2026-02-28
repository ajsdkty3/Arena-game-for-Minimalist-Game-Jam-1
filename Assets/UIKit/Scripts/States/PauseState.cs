using UnityEngine;
using UIKit.StateMachine;

namespace UIKit.States {
    public class PauseState : IUIState {
        public UIStateId Id => UIStateId.Pause;

        readonly GameObject _menu, _pause, _settings;

        public PauseState(GameObject menu, GameObject pause, GameObject settings) {
            _menu = menu;
            _pause = pause;
            _settings = settings;
        }

        public void Enter() {
            Time.timeScale = 0f;
            _menu.SetActive(false);
            _pause.SetActive(true);
            _settings.SetActive(false);
        }

        public void Exit() {
            Time.timeScale = 1f;
            _pause.SetActive(false);
        }
    }
}