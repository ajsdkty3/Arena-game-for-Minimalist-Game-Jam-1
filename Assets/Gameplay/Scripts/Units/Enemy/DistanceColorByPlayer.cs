using UnityEngine;
using Gameplay.Units.Movement;

[RequireComponent(typeof(MeshRenderer))]
public class DistanceColorByPlayer_Mesh : MonoBehaviour {
    [Header("Player")]
    public string playerTag = "Player";

    [Header("Colors")]
    public Color farColor = Color.black;
    public Color nearColor = Color.red;

    [Header("Distance")]
    public float maxDistance = 6f;

    [Header("Material Color Property")]
    public string colorProperty = "_BaseColor";

    [Header("Delay Lock (optional)")]
    public bool lockColorUntilMovementActive = true;

    MeshRenderer _mr;
    MaterialPropertyBlock _mpb;
    Transform _player;
    int _colorId;

    // ✅ 在父物体（logic）上找 DelayedInertiaSeekMovement
    DelayedInertiaSeekMovement _delayedMove;

    void Awake() {
        _mr = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
        _colorId = Shader.PropertyToID(colorProperty);

        // Visual 在子物体时，用 InParent 找 logic
        _delayedMove = GetComponentInParent<DelayedInertiaSeekMovement>();
    }

    void OnEnable() {
        TryFindPlayer();
        ApplyColor(farColor);
    }

    void Update() {
        if (lockColorUntilMovementActive && _delayedMove != null && !_delayedMove.IsActive) {
            ApplyColor(farColor);
            return;
        }

        if (_player == null) {
            TryFindPlayer();
            if (_player == null)
                return;
        }

        // Visual 子物体跟着移动，所以用 transform.position 没问题
        float safeMax = Mathf.Max(0.0001f, maxDistance);
        float dist = Vector2.Distance(transform.position, _player.position);
        float t = 1f - Mathf.Clamp01(dist / safeMax);

        Color c = Color.Lerp(farColor, nearColor, t);
        ApplyColor(c);
    }

    void TryFindPlayer() {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            _player = go.transform;
    }

    void ApplyColor(Color c) {
        _mr.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorId, c);
        _mr.SetPropertyBlock(_mpb);
    }
}

/*using UnityEngine;
using Gameplay.Units.Movement;

[RequireComponent(typeof(MeshRenderer))]
public class DistanceBlendByPlayer_Mesh : MonoBehaviour {
    [Header("Player")]
    public string playerTag = "Player";

    [Header("Distance")]
    public float maxDistance = 6f;

    [Header("Shader Property (match Shader Graph Reference)")]
    public string distanceFactorProperty = "_DistanceFactor";

    [Header("Blend Tuning")]
    [Tooltip(">1 makes it reach near sooner, <1 makes it slower.")]
    public float distanceFactorMultiplier = 1f;

    [Tooltip("1=linear, 2=stronger near, 0.5=stronger far.")]
    public float distanceFactorPower = 1f;

    [Header("Delay Lock (optional)")]
    public bool lockUntilMovementActive = true;

    MeshRenderer _mr;
    MaterialPropertyBlock _mpb;
    Transform _player;

    int _distId;
    DelayedInertiaSeekMovement _delayedMove;

    void Awake() {
        _mr = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
        _distId = Shader.PropertyToID(distanceFactorProperty);

        _delayedMove = GetComponentInParent<DelayedInertiaSeekMovement>();
    }

    void OnEnable() {
        TryFindPlayer();
        Apply(0f);
    }

    void Update() {
        if (lockUntilMovementActive && _delayedMove != null && !_delayedMove.IsActive) {
            Apply(0f);
            return;
        }

        if (_player == null) {
            TryFindPlayer();
            if (_player == null)
                return;
        }

        float dist = Vector2.Distance(transform.position, _player.position);

        float t = 1f - Mathf.Clamp01(dist / Mathf.Max(0.0001f, maxDistance));

        t = Mathf.Clamp01(t * distanceFactorMultiplier);

        if (distanceFactorPower != 1f && distanceFactorPower > 0.0001f)
            t = Mathf.Pow(t, distanceFactorPower);

        Apply(t);
    }

    void TryFindPlayer() {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null)
            _player = go.transform;
    }

    void Apply(float t) {
        _mr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_distId, t);
        _mr.SetPropertyBlock(_mpb);
    }
}
*/