using UnityEngine;

public class CameraShake2D : MonoBehaviour {
    public static CameraShake2D I { get; private set; }

    Vector3 _baseLocalPos;
    float _shakeTime;
    float _shakeStrength;

    void Awake() {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        _baseLocalPos = transform.localPosition;
    }

    void LateUpdate() {
        if (_shakeTime > 0f) {
            _shakeTime -= Time.unscaledDeltaTime;

            Vector2 offset = Random.insideUnitCircle * _shakeStrength;
            transform.localPosition = _baseLocalPos + (Vector3)offset;
        } else {
            transform.localPosition = _baseLocalPos;
        }
    }

    public void Shake(float duration, float strength) {
        _shakeTime = Mathf.Max(_shakeTime, duration);
        _shakeStrength = Mathf.Max(_shakeStrength, strength);
    }
}