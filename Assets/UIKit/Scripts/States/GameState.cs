using UnityEngine;
using UIKit.StateMachine;

namespace UIKit.States {
    public class GameState : IUIState {
        public UIStateId Id => UIStateId.Game;

        readonly GameObject _menu, _pause, _settings;

        public GameState(GameObject menu, GameObject pause, GameObject settings) {
            _menu = menu;
            _pause = pause;
            _settings = settings;
        }

        public void Enter() {
            Time.timeScale = 1f;
            _menu.SetActive(false);
            _pause.SetActive(false);
            _settings.SetActive(false);
        }

        public void Exit() { }
    }
}