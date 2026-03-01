using UnityEngine;
using Gameplay.Units.Movement;

public class DistanceEnableParticleByPlayer : MonoBehaviour {
    [Header("Player")]
    public string playerTag = "Player";

    [Header("Particle")]
    public ParticleSystem ps;

    [Header("Distance")]
    public float enableDistance = 3f;
    public float disableDistance = 3.1f;

    [Header("Size Boost")]
    [Tooltip("在这个距离内放大粒子尺寸")]
    public float boostDistance = 1.5f;

    [Tooltip("放大倍数")]
    public float sizeMultiplier = 2f;

    [Header("Delay Lock (optional)")]
    public bool lockUntilMovementActive = true;

    Transform _player;
    bool _on;
    bool _boosted;

    DelayedInertiaSeekMovement _delayedMove;

    float _baseSizeMultiplier;

    void Awake() {
        if (ps == null)
            ps = GetComponentInChildren<ParticleSystem>(true);

        _delayedMove = GetComponentInParent<DelayedInertiaSeekMovement>();

        if (ps != null) {
            var main = ps.main;
            _baseSizeMultiplier = main.startSizeMultiplier;
        }
    }

    void OnEnable() {
        TryFindPlayer();
        SetOn(false, true);
        ResetSize();
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

        // 控制开启/关闭
        if (!_on && d <= enableDistance)
            SetOn(true);
        else if (_on && d >= disableDistance)
            SetOn(false);

        // 控制尺寸放大
        if (d <= boostDistance && !_boosted) {
            ApplySize(_baseSizeMultiplier * sizeMultiplier);
            _boosted = true;
        } else if (d > boostDistance && _boosted) {
            ResetSize();
            _boosted = false;
        }
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
            if (ps.isPlaying || ps.particleCount > 0)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void ApplySize(float value) {
        var main = ps.main;
        main.startSizeMultiplier = value;
    }

    void ResetSize() {
        var main = ps.main;
        main.startSizeMultiplier = _baseSizeMultiplier;
    }
}