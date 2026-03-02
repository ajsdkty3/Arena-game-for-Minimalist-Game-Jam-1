using UnityEngine;

public class CameraShake2D : MonoBehaviour {
    public static CameraShake2D I { get; private set; }

    Vector3 _baseLocalPos;

    float _shakeTime;
    float _shakeDuration;
    float _shakeStrength;

    void Awake() {
        if (I != null && I != this) {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    void OnEnable() {
        _baseLocalPos = transform.localPosition;
    }

    void LateUpdate() {
        if (_shakeTime > 0f) {
            _shakeTime -= Time.unscaledDeltaTime;

            float normalized = _shakeTime / _shakeDuration; // 逐渐减弱
            float currentStrength = _shakeStrength * normalized;

            Vector2 offset = Random.insideUnitCircle * currentStrength;
            transform.localPosition = _baseLocalPos + (Vector3)offset;
        } else {
            transform.localPosition = _baseLocalPos;
        }
    }

    public void Shake(float duration, float strength) {
        if (duration <= 0f)
            return;

        _shakeDuration = duration;
        _shakeTime = duration;

        _shakeStrength = Mathf.Max(_shakeStrength, strength);
    }
}