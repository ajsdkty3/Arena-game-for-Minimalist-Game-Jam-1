using UnityEngine;
using Gameplay.Units.Movement;

public class DistanceEnableParticleByPlayer : MonoBehaviour {
    [Header("Player")]
    public string playerTag = "Player";

    [Header("Particle")]
    [Tooltip("拖进要控制的 ParticleSystem（一般是 visual 子物体的粒子）")]
    public ParticleSystem ps;

    [Header("Distance")]
    [Tooltip("进入这个距离就开启")]
    public float enableDistance = 3f;

    [Tooltip("离开到这个距离才关闭（建议 > enableDistance，避免抖动）")]
    public float disableDistance = 3.1f;

    [Header("Delay Lock (optional)")]
    public bool lockUntilMovementActive = true;

    Transform _player;
    bool _on;

    // optional: 在父物体（logic）上找
    DelayedInertiaSeekMovement _delayedMove;

    void Awake() {
        if (ps == null)
            ps = GetComponentInChildren<ParticleSystem>(true);

        _delayedMove = GetComponentInParent<DelayedInertiaSeekMovement>();
    }

    void OnEnable() {
        TryFindPlayer();
        SetOn(false, true); // 强制关一次
    }

    void Update() {
        if (ps == null)
            return;

        if (lockUntilMovementActive && _delayedMove != null && !_delayedMove.IsActive) {
            SetOn(false);
            return;
        }

        if (_player == null) {
            TryFindPlayer();
            if (_player == null)
                return;
        }

        float d = Vector2.Distance(transform.position, _player.position);

        if (!_on && d <= enableDistance)
            SetOn(true);
        else if (_on && d >= disableDistance)
            SetOn(false);
    }

    void TryFindPlayer() {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            _player = go.transform;
    }

    void SetOn(bool on, bool force = false) {
        if (!force && _on == on)
            return;

        _on = on;

        if (_on) {
            if (!ps.isPlaying)
                ps.Play(true);
        } else {
            // StopEmittingAndClear：关掉并清空残留粒子
            if (ps.isPlaying || ps.particleCount > 0)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}