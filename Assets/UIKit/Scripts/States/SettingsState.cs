using UnityEngine;
using UIKit.StateMachine;

namespace UIKit.States {
    public class SettingsState : IUIState {
        public UIStateId Id => UIStateId.Settings;

        readonly GameObject _settings;

        public SettingsState(GameObject settings) {
            _settings = settings;
        }

        public void Enter() {
            _settings.SetActive(true);
        }

        public void Exit() {
            _settings.SetActive(false);
        }
    }
}