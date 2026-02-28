using UnityEngine;
using UnityEngine.InputSystem;

namespace UIKit.Input {
    public class InputRouter : MonoBehaviour {
        [Header("Assign .inputactions asset")]
        public InputActionAsset actions;

        [Header("Action Map Names")]
        public string globalMap = "Global";
        public string gameplayMap = "Gameplay";
        public string uiMap = "UI";

        public InputActionMap Global { get; private set; }
        public InputActionMap Gameplay { get; private set; }
        public InputActionMap UI { get; private set; }

        public InputAction PauseAction { get; private set; }

        void Awake() {
            Global = actions.FindActionMap(globalMap, true);
            Gameplay = actions.FindActionMap(gameplayMap, true);
            UI = actions.FindActionMap(uiMap, true);

            PauseAction = actions.FindAction("Pause", true);
        }

        void OnEnable() {
            // Global 永远开
            Global.Enable();
        }

        void OnDisable() {
            // 全关掉
            Global.Disable();
            Gameplay.Disable();
            UI.Disable();
        }

        public void SetDomainGameplay() {
            Gameplay.Enable();
            UI.Disable();
        }

        public void SetDomainUI() {
            UI.Enable();
            Gameplay.Disable();
        }
    }
}