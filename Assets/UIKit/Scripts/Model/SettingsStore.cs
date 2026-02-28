using UnityEngine;

namespace UIKit.Model {
    public static class SettingsStore {
        // Graphics
        const string KEY_FULLSCREEN = "uikit.fullscreen";
        const string KEY_RES_INDEX = "uikit.resolutionIndex";

        // Audio (0..1 slider values)
        const string KEY_MASTER = "uikit.audio.master";
        const string KEY_MUSIC = "uikit.audio.music";
        const string KEY_SFX = "uikit.audio.sfx";

        public static bool LoadFullscreen(bool defaultValue = true) {
            return PlayerPrefs.GetInt(KEY_FULLSCREEN, defaultValue ? 1 : 0) == 1;
        }

        public static int LoadResolutionIndex(int defaultValue = 0) {
            return PlayerPrefs.GetInt(KEY_RES_INDEX, defaultValue);
        }

        public static void SaveFullscreen(bool value) {
            PlayerPrefs.SetInt(KEY_FULLSCREEN, value ? 1 : 0);
        }

        public static void SaveResolutionIndex(int index) {
            PlayerPrefs.SetInt(KEY_RES_INDEX, index);
        }

        public static float LoadMaster(float def = 1f) => PlayerPrefs.GetFloat(KEY_MASTER, def);
        public static float LoadMusic(float def = 1f) => PlayerPrefs.GetFloat(KEY_MUSIC, def);
        public static float LoadSfx(float def = 1f) => PlayerPrefs.GetFloat(KEY_SFX, def);

        public static void SaveMaster(float v) => PlayerPrefs.SetFloat(KEY_MASTER, v);
        public static void SaveMusic(float v) => PlayerPrefs.SetFloat(KEY_MUSIC, v);
        public static void SaveSfx(float v) => PlayerPrefs.SetFloat(KEY_SFX, v);

        public static void Flush() => PlayerPrefs.Save();
    }
}