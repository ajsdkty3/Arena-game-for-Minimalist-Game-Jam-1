using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PolygonShape : MonoBehaviour {
    [Header("Shape (fixed topology)")]
    [Min(3)] public int vertexCount = 10;         // ✅ 固定顶点数（建议 8~16）
    [Min(0.01f)] public float size = 1f;
    public Vector2 jitterRange = new Vector2(0.2f, 0.6f);

    [Header("Animation (CPU)")]
    [Range(0f, 0.5f)] public float breatheAmp = 0.08f;  // 整体呼吸幅度（相对半径）
    public float breatheSpeed = 1.5f;

    [Range(0f, 0.5f)] public float wobbleAmp = 0.06f;   // 每点抖动幅度（相对半径）
    public float wobbleSpeed = 2.5f;

    MeshFilter _mf;
    Mesh _mesh;

    // cached (no GC)
    float[] _baseAngle;
    float[] _baseRadius;
    float[] _phase;

    Vector3[] _verts;   // n + 1 center
    int[] _tris;

    float _angleOffset;
    float _jitter01;

    void Awake() {
        _mf = GetComponent<MeshFilter>();

        _mesh = new Mesh { name = "PolygonMesh_Animated" };
        _mesh.MarkDynamic();
        _mf.sharedMesh = _mesh;
    }

    void OnEnable() {
        Regenerate();
    }

    void Update() {
        Animate(Time.time);
    }

    public void Regenerate() {
        vertexCount = Mathf.Max(3, vertexCount);

        EnsureBuffers(vertexCount);

        _jitter01 = Random.Range(jitterRange.x, jitterRange.y);
        _angleOffset = Random.Range(0f, Mathf.PI * 2f);

        float step = Mathf.PI * 2f / vertexCount;

        for (int i = 0; i < vertexCount; i++) {
            float a = _angleOffset + step * i + Random.Range(-step * 0.25f, step * 0.25f);
            float r = size * Mathf.Lerp(1f, Random.Range(0.4f, 1.2f), _jitter01);

            _baseAngle[i] = a;
            _baseRadius[i] = r;
            _phase[i] = Random.Range(0f, Mathf.PI * 2f);
        }

        BuildStaticMesh(vertexCount);
        Animate(Time.time); // 立刻刷新一帧
    }

    void EnsureBuffers(int n) {
        if (_baseAngle != null && _baseAngle.Length == n)
            return;

        _baseAngle = new float[n];
        _baseRadius = new float[n];
        _phase = new float[n];

        _verts = new Vector3[n + 1];  // + center
        _tris = new int[n * 3];       // fan
    }

    void BuildStaticMesh(int n) {
        int center = n;

        int t = 0;
        for (int i = 0; i < n; i++) {
            int next = (i + 1) % n;
            _tris[t++] = center;
            _tris[t++] = i;
            _tris[t++] = next;
        }

        _mesh.Clear(false);
        _mesh.vertices = _verts;     // 先占位
        _mesh.triangles = _tris;
        _mesh.RecalculateNormals();

        // ✅ 预扩大 bounds，避免顶点动画被裁剪
        float b = size * 3f;
        _mesh.bounds = new Bounds(Vector3.zero, new Vector3(b, b, 1f));
    }

    void Animate(float time) {
        int n = vertexCount;
        if (n < 3)
            return;

        // center vertex
        _verts[n] = Vector3.zero;

        // 整体呼吸
        float breathe = 1f + breatheAmp * Mathf.Sin(time * breatheSpeed);

        for (int i = 0; i < n; i++) {
            // 每点抖动（不同相位，看起来更“活”）
            float wobble = 1f + wobbleAmp * Mathf.Sin(time * wobbleSpeed + _phase[i]);

            float r = _baseRadius[i] * breathe * wobble;
            float a = _baseAngle[i];

            _verts[i] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
        }

        _mesh.vertices = _verts;
        // 不要每帧 RecalculateBounds/Normals：省很多
    }
}