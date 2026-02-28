using System.Collections;
using UnityEngine;

namespace Gameplay.VFX {
    /// <summary>
    /// Visual-only hit reaction: flash + squash.
    /// Put this on the visual child (enemyImage).
    /// </summary>
    public class EnemyHitReact : MonoBehaviour {

        [Header("Renderer (optional)")]
        public Renderer targetRenderer;             // SpriteRenderer / MeshRenderer 都行
        public string colorProperty = "_BaseColor"; // ShaderGraph 常用。若用 SpriteRenderer color 可改下面设置

        [Header("Flash")]
        public Color hitColor = new Color(1f, 0.95f, 0.6f, 1f); // 偏白偏黄
        public float flashInTime = 0.1f;
        public float flashOutTime = 0.1f;

        [Header("Squash")]
        public Vector3 squashScale = new Vector3(0.85f, 1.15f, 1f);
        public float squashInTime = 0.06f;
        public float squashOutTime = 0.10f;

        MaterialPropertyBlock _mpb;
        int _colorId;
        Color _baseColor;
        bool _hasColorProperty;

        Vector3 _baseLocalScale;

        Coroutine _flashCo;
        Coroutine _squashCo;

        void Awake() {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            _baseLocalScale = transform.localScale;

            // Setup color flash
            if (targetRenderer != null && !string.IsNullOrEmpty(colorProperty)) {
                _mpb = new MaterialPropertyBlock();
                _colorId = Shader.PropertyToID(colorProperty);

                // 尝试从 PropertyBlock 读（可能是默认空的）
                targetRenderer.GetPropertyBlock(_mpb);
                _baseColor = _mpb.GetColor(_colorId);

                // 如果读出来是默认 (0,0,0,0)，再从 sharedMaterial 读一次
                if (_baseColor.a == 0f && targetRenderer.sharedMaterial != null) {
                    if (targetRenderer.sharedMaterial.HasProperty(_colorId)) {
                        _baseColor = targetRenderer.sharedMaterial.GetColor(_colorId);
                        _hasColorProperty = true;
                    } else {
                        _hasColorProperty = false;
                    }
                } else {
                    // 这时不确定 shader 是否有该属性，但先认为有
                    _hasColorProperty = true;
                }
            }
        }

        void OnEnable() {
            // pooling: 防止上次被打闪的状态残留
            transform.localScale = _baseLocalScale;
            SetColor(_baseColor);
        }

        void OnDisable() {
            // pooling: 停掉协程，避免下次启用时状态错乱
            if (_flashCo != null)
                StopCoroutine(_flashCo);
            if (_squashCo != null)
                StopCoroutine(_squashCo);
            _flashCo = null;
            _squashCo = null;

            transform.localScale = _baseLocalScale;
            SetColor(_baseColor);
        }

        /// <summary>
        /// Call this when enemy takes damage.
        /// </summary>
        public void Play() {
            if (_flashCo != null)
                StopCoroutine(_flashCo);
            if (_squashCo != null)
                StopCoroutine(_squashCo);

            _flashCo = StartCoroutine(FlashRoutine());
            _squashCo = StartCoroutine(SquashRoutine());
        }

        IEnumerator FlashRoutine() {
            if (targetRenderer == null || _mpb == null || !_hasColorProperty) {
                yield break;
            }

            float t = 0f;

            // in
            while (t < flashInTime) {
                t += Time.deltaTime;
                float a = flashInTime <= 0f ? 1f : Mathf.Clamp01(t / flashInTime);
                SetColor(Color.Lerp(_baseColor, hitColor, a));
                yield return null;
            }

            t = 0f;

            // out
            while (t < flashOutTime) {
                t += Time.deltaTime;
                float a = flashOutTime <= 0f ? 1f : Mathf.Clamp01(t / flashOutTime);
                SetColor(Color.Lerp(hitColor, _baseColor, a));
                yield return null;
            }

            SetColor(_baseColor);
        }

        IEnumerator SquashRoutine() {
            Vector3 from = _baseLocalScale;
            Vector3 to = new Vector3(
                _baseLocalScale.x * squashScale.x,
                _baseLocalScale.y * squashScale.y,
                _baseLocalScale.z * squashScale.z
            );

            float t = 0f;

            // in
            while (t < squashInTime) {
                t += Time.deltaTime;
                float a = squashInTime <= 0f ? 1f : Mathf.Clamp01(t / squashInTime);
                transform.localScale = Vector3.Lerp(from, to, Smooth(a));
                yield return null;
            }

            t = 0f;

            // out
            while (t < squashOutTime) {
                t += Time.deltaTime;
                float a = squashOutTime <= 0f ? 1f : Mathf.Clamp01(t / squashOutTime);
                transform.localScale = Vector3.Lerp(to, from, Smooth(a));
                yield return null;
            }

            transform.localScale = _baseLocalScale;
        }

        float Smooth(float x) => x * x * (3f - 2f * x); // SmoothStep

        void SetColor(Color c) {
            if (targetRenderer == null || _mpb == null || !_hasColorProperty)
                return;

            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, c);
            targetRenderer.SetPropertyBlock(_mpb);
        }
    }
}