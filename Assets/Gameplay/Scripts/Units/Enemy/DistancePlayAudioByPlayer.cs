using UnityEngine;
using Gameplay.Units.Movement;

public class DistancePlayAudioByPlayer_OneShot2D : MonoBehaviour {

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Clips")]
    public AudioClip startClip;
    public AudioClip nearClip;
    public AudioClip farClip;

    [Header("2D Volume/Pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    public bool randomPitch = true;
    public float pitchMin = 0.95f;
    public float pitchMax = 1.05f;

    [Header("Cooldown")]
    public float cooldown = 0.25f;
    float _nextPlayTime;

    [Header("Distance")]
    public float enableDistance = 3f;
    public float disableDistance = 3.1f;

    [Header("Delay Lock (optional)")]
    public bool lockUntilMovementActive = true;

    Transform _player;
    bool _near;

    DelayedInertiaSeekMovement _delayedMove;

    void Awake() {
        _delayedMove = GetComponentInParent<DelayedInertiaSeekMovement>();
    }

    void OnEnable() {
        TryFindPlayer();
        _near = false;
        _nextPlayTime = 0f;
        Play(startClip);
    }

    void Update() {
        if (lockUntilMovementActive && _delayedMove != null && !_delayedMove.IsActive)
            return;

        if (_player == null) {
            TryFindPlayer();
            if (_player == null)
                return;
        }

        float d = Vector2.Distance(transform.position, _player.position);

        if (!_near && d <= enableDistance) {
            _near = true;
            Play(nearClip);
        } else if (_near && d >= disableDistance) {
            _near = false;
            Play(farClip);
        }
    }

    void TryFindPlayer() {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            _player = go.transform;
    }

    void Play(AudioClip clip) {
        if (clip == null)
            return;

        if (Time.time < _nextPlayTime)
            return;

        if (SfxOneShotHub2D.I == null)
            return;

        float pitch = randomPitch ? Random.Range(pitchMin, pitchMax) : 1f;

        SfxOneShotHub2D.I.Play(clip, volume, pitch);

        _nextPlayTime = Time.time + cooldown;
    }
}