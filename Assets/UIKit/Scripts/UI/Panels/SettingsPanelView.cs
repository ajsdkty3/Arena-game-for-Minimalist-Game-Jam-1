using System;
using TMPro;
using UIKit.Model;
using UnityEngine;
using UnityEngine.UI;

namespace UIKit.UI.Panels {
    public class SettingsPanelView : MonoBehaviour {
        public event Action BackClicked;

        [Header("Audio")]
        public Slider master;
        public Slider music;
        public Slider sfx;

        [Header("Graphics")]
        public Toggle fullscreen;
        public TMP_Dropdown resolution;

        bool _suppress;
        SettingsModel _model;

        public void Bind(SettingsModel model) {
            _model = model;

            // Dropdown options
            if (resolution != null) {
                resolution.ClearOptions();
                var opts = new System.Collections.Generic.List<string>();
                foreach (var r in _model.Resolutions)
                    opts.Add($"{r.x} x {r.y}");
                resolution.AddOptions(opts);
            }

            // Model -> UI (Reactive)
            _model.Master.Changed += v => { if (_suppress) return; SetSlider(master, v); };
            _model.Music.Changed += v => { if (_suppress) return; SetSlider(music, v); };
            _model.Sfx.Changed += v => { if (_suppress) return; SetSlider(sfx, v); };

            _model.Fullscreen.Changed += v => {
                if (_suppress)
                    return;
                if (fullscreen)
                    fullscreen.isOn = v;
            };

            _model.ResolutionIndex.Changed += i => {
                if (_suppress)
                    return;
                if (resolution) {
                    resolution.value = i;
                    resolution.RefreshShownValue();
                }
            };

            // UI -> Model
            if (master)
                master.onValueChanged.AddListener(v => { if (_suppress) return; _model.Master.Value = v; });

            if (music)
                music.onValueChanged.AddListener(v => { if (_suppress) return; _model.Music.Value = v; });

            if (sfx)
                sfx.onValueChanged.AddListener(v => { if (_suppress) return; _model.Sfx.Value = v; });

            if (fullscreen)
                fullscreen.onValueChanged.AddListener(v => {
                    if (_suppress)
                        return;
                    _model.Fullscreen.Value = v;
                });

            if (resolution)
                resolution.onValueChanged.AddListener(i => {
                    if (_suppress)
                        return;
                    _model.ResolutionIndex.Value = i;
                });
        }

        public void RefreshAllFromModel() {
            if (_model == null)
                return;

            _suppress = true;

            SetSlider(master, _model.Master.Value);
            SetSlider(music, _model.Music.Value);
            SetSlider(sfx, _model.Sfx.Value);

            if (fullscreen)
                fullscreen.isOn = _model.Fullscreen.Value;

            if (resolution) {
                resolution.value = Mathf.Clamp(_model.ResolutionIndex.Value, 0, _model.Resolutions.Length - 1);
                resolution.RefreshShownValue();
            }

            _suppress = false;
        }

        static void SetSlider(Slider s, float v) {
            if (s == null)
                return;
            s.value = v;
        }

        public void OnBackClicked() => BackClicked?.Invoke();
    }
}