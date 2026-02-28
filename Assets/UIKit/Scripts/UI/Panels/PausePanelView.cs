using System;
using UnityEngine;

namespace UIKit.UI.Panels {
    public class PausePanelView : MonoBehaviour {
        public event Action ResumeClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;

        public void OnResumeClicked() => ResumeClicked?.Invoke();
        public void OnSettingsClicked() => SettingsClicked?.Invoke();
        public void OnQuitClicked() => QuitClicked?.Invoke();
    }
}