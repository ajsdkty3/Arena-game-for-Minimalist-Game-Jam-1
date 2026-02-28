using UnityEngine;

public class HolyLightFollow : MonoBehaviour {

    public Transform target;   // 玩家

    [Header("Distance")]
    public float followDistance = 2f;

    [Header("Lag")]
    public float smoothTime = 0.15f;
    public float maxSpeed = 999f;

    Vector3 _vel;

    void LateUpdate() {
        if (!target)
            return;

        // 玩家“前方”默认是 right（如果你用 up 朝前就改成 target.up）
        Vector3 backward = -target.right.normalized;

        Vector3 desired = target.position + backward * followDistance;

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