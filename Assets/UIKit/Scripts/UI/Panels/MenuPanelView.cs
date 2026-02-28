using System;
using UnityEngine;

namespace UIKit.UI.Panels {
    public class MenuPanelView : MonoBehaviour {
        public event Action StartClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;

        public void OnStartClicked() => StartClicked?.Invoke();
        public void OnSettingsClicked() => SettingsClicked?.Invoke();
        public void OnQuitClicked() => QuitClicked?.Invoke();
    }
}