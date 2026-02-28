using UnityEngine;
using UnityEngine.Audio;
using UIKit.Reactive;

namespace UIKit.Model {
    public class SettingsModel {
        // ========== Audio (0..1 slider values) ==========
        public readonly Observable<float> Master = new(1f);
        public readonly Observable<float> Music = new(1f);
        public readonly Observable<float> Sfx = new(1f);

        // ========== Graphics ==========
        public readonly Observable<bool> Fullscreen;
        public readonly Observable<int> ResolutionIndex;

        public readonly Vector2Int[] Resolutions =
        {
            new(1280, 720),
            new(1600, 900),
            new(1920, 1080),
        };

        readonly AudioMixer _mixer;

        // Exposed parameter names in AudioMixer
        const string MASTER_VOL = "MasterVol";
        const string MUSIC_VOL = "MusicVol";
        const string SFX_VOL = "SFXVol";

        public SettingsModel(AudioMixer mixer) {
            _mixer = mixer;

            // ===== Load persisted values =====
            Fullscreen = new Observable<bool>(SettingsStore.LoadFullscreen(true));
            ResolutionIndex = new Observable<int>(SettingsStore.LoadResolutionIndex(0));

            Master.Value = SettingsStore.LoadMaster(1f);
            Music.Value = SettingsStore.LoadMusic(1f);
            Sfx.Value = SettingsStore.LoadSfx(1f);

            // ===== Auto save + apply: Graphics =====
            Fullscreen.Changed += v => {
                SettingsStore.SaveFullscreen(v);
                ApplyGraphics();
                SettingsStore.Flush();
            };

            ResolutionIndex.Changed += i => {
                SettingsStore.SaveResolutionIndex(i);
                ApplyGraphics();
                SettingsStore.Flush();
            };

            // ===== Auto save + apply: Audio =====
            Master.Changed += v => {
                SettingsStore.SaveMaster(v);
                ApplyAudio();
                SettingsStore.Flush();
            };

            Music.Changed += v => {
                SettingsStore.SaveMusic(v);
                ApplyAudio();
                SettingsStore.Flush();
            };

            Sfx.Changed += v => {
                SettingsStore.SaveSfx(v);
                ApplyAudio();
                SettingsStore.Flush();
            };

            // ===== Apply once on boot =====
            ApplyGraphics();
            ApplyAudio();
        }

        public void ApplyGraphics() {
            int idx = Mathf.Clamp(ResolutionIndex.Value, 0, Resolutions.Length - 1);
            var r = Resolutions[idx];
            Screen.SetResolution(r.x, r.y, Fullscreen.Value);
        }

        public void ApplyAudio() {
            if (_mixer == null)
                return;

            float mDb = ToDb(Master.Value);
            float muDb = ToDb(Music.Value);
            float sDb = ToDb(Sfx.Value);

            _mixer.SetFloat(MASTER_VOL, mDb);
            _mixer.SetFloat(MUSIC_VOL, muDb);
            _mixer.SetFloat(SFX_VOL, sDb);

            _mixer.GetFloat(MASTER_VOL, out var curM);
            _mixer.GetFloat(MUSIC_VOL, out var curMu);
            _mixer.GetFloat(SFX_VOL, out var curS);
        }

        static float ToDb(float v01) {
            v01 = Mathf.Clamp01(v01);
            if (v01 <= 0.0001f)
                return -80f; // ~silent
            return Mathf.Log10(v01) * 20f;
        }
    }
}