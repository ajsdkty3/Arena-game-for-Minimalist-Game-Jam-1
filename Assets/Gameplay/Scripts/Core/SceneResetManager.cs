using UnityEngine;

public class SceneResetManager : MonoBehaviour {

    void Awake() {
        // 恢复全局时间与音频
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}