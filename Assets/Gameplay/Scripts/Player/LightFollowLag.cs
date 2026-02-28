using UnityEngine;

public class LightFollowLag : MonoBehaviour {
    public Transform target;
    public Vector3 offset = Vector3.zero;

    [Header("Lag")]
    public float smoothTime = 0.15f;   // 越大越“拖”
    public float maxSpeed = 999f;      // 需要限制最大追赶速度时用

    Vector3 _vel;

    void LateUpdate() {
        if (!target)
            return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref _vel,
            smoothTime,
            maxSpeed,
            Time.deltaTime
        );
    }
}