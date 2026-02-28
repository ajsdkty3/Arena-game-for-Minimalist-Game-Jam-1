using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLookAtMouse : MonoBehaviour {

    public Transform playerImage;
    public float angleOffsetDeg = 0f;

    Camera _cam;

    void Awake() {
        _cam = Camera.main;
    }

    void Update() {
        if (!playerImage || !_cam)
            return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = playerImage.position.z;

        Vector2 dir = mouseWorld - playerImage.position;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg;
        playerImage.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}