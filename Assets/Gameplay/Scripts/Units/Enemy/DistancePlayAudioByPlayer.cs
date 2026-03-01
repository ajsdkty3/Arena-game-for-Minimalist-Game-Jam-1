using UnityEngine;
using Gameplay.Units.Movement;

public class DistancePlayAudioByPlayer_OneShot2D_Debug : MonoBehaviour {
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

    [Header("Debug")]
    public bool debugLogs = true;
    public bool debugEverySecond = false;
    float _nextDbg;

    Transform _player;
    bool _near;

    DelayedInertiaSeekMovement _delayedMove;

    void Awake() {
        _delayedMove = GetComponentInParent<DelayedInertiaSeekMovement>();
        if (debugLogs)
            Debug.Log($"[EnemySfx] Awake on {name}");
    }

    void OnEnable() {
        TryFindPlayer();

        _near = false;
        _nextPlayTime = 0f;

        if (debugLogs)
            Debug.Log($"[EnemySfx] OnEnable {name} playerFound={_player != null} hub={(SfxOneShotHub2D.I != null)}");

        Play(startClip, "START");
    }

    void Update() {
        if (lockUntilMovementActive && _delayedMove != null && !_delayedMove.IsActive) {
            if (debugEverySecond && Time.time >= _nextDbg) {
                _nextDbg = Time.time + 1f;
                Debug.Log($"[EnemySfx] locked (movement inactive) {name}");
            }
            return;
        }

        if (_player == null) {
            TryFindPlayer();
            if (_player == null) {
                if (debugEverySecond && Time.time >= _nextDbg) {
                    _nextDbg = Time.time + 1f;
                    Debug.LogWarning($"[EnemySfx] Player not found tag='{playerTag}' {name}");
                }
                return;
            }
        }

        float d = Vector2.Distance(transform.position, _player.position);

        if (debugEverySecond && Time.time >= _nextDbg) {
            _nextDbg = Time.time + 1f;
            Debug.Log($"[EnemySfx] d={d:F2} near={_near} {name}");
        }

        if (!_near && d <= enableDistance) {
            _near = true;
            if (debugLogs)
                Debug.Log($"[EnemySfx] ENTER near d={d:F2} {name}");
            Play(nearClip, "NEAR_ENTER");
        } else if (_near && d >= disableDistance) {
            _near = false;
            if (debugLogs)
                Debug.Log($"[EnemySfx] EXIT near d={d:F2} {name}");
            Play(farClip, "NEAR_EXIT");
        }
    }

    void TryFindPlayer() {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            _player = go.transform;
    }

    void Play(AudioClip clip, string tag) {
        if (clip == null) {
            if (debugLogs)
                Debug.LogWarning($"[EnemySfx] clip NULL tag={tag} {name}");
            return;
        }

        if (Time.time < _nextPlayTime) {
            if (debugLogs)
                Debug.Log($"[EnemySfx] cooldown block tag={tag} {name}");
            return;
        }

        if (SfxOneShotHub2D.I == null) {
            Debug.LogError($"[EnemySfx] NO HUB in scene. Add SfxOneShotHub2D. tag={tag} {name}");
            return;
        }

        float pitch = 1f;
        if (randomPitch)
            pitch = Random.Range(pitchMin, pitchMax);

        SfxOneShotHub2D.I.Play(clip, volume, pitch, $"{tag}/{name}");

        _nextPlayTime = Time.time + cooldown;
    }
}