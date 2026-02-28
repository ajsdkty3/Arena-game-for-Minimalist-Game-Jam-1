using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal; // ✅ Light2D

namespace Gameplay.Player {
    public class PlayerJump : MonoBehaviour {
        [Header("Input")]
        public InputActionReference jump;

        [Header("Visual")]
        public Transform playerImage;
        public Transform shadow;

        [Header("Shadow Tuning")]
        [Tooltip("最高点时阴影缩放倍率（1 = 不变，0.6 = 缩小到60%）")]
        [Range(0f, 1f)]
        public float shadowScaleMultiplierAtApex = 0.6f;

        [Header("Jump Light (URP 2D)")]
        public Light2D playerLight;
        [Min(0f)] public float lightIntensityAddAtApex = 0.8f; // 最高点额外强度
        [Min(0f)] public float lightRadiusAddAtApex = 1.0f;    // 最高点额外范围(outerRadius)
        [Range(0f, 5f)] public float lightResponse = 1f;        // 高度曲线响应(>1 更后段才变强)

        [Header("Collision")]
        public Collider2D playerCollider;

        [Header("Jump Tuning")]
        [Min(0.05f)] public float jumpDuration = 0.35f;
        [Min(0f)] public float jumpHeight = 0.7f;

        [Min(0f)] public float invulnEdgeTime = 0.06f;

        bool _jumping;
        Vector3 _imgBaseLocalPos;
        Vector3 _shadowBaseLocalPos;
        Vector3 _shadowBaseScale;
        Coroutine _co;

        float _lightBaseIntensity;
        float _lightBaseOuterRadius;
        bool _lightCached;

        void Awake() {
            if (playerCollider == null)
                playerCollider = GetComponent<Collider2D>();

            if (playerImage != null)
                _imgBaseLocalPos = playerImage.localPosition;

            if (shadow != null) {
                _shadowBaseLocalPos = shadow.localPosition;
                _shadowBaseScale = shadow.localScale;
            }

            CacheLightBase();
        }

        void CacheLightBase() {
            if (playerLight == null)
                return;
            _lightBaseIntensity = playerLight.intensity;
            _lightBaseOuterRadius = playerLight.pointLightOuterRadius; // ✅ URP 2D 的范围
            _lightCached = true;
        }

        void OnEnable() {
            if (jump != null) {
                jump.action.Enable();
                jump.action.performed += OnJumpPerformed;
            }
        }

        void OnDisable() {
            if (jump != null) {
                jump.action.performed -= OnJumpPerformed;
                jump.action.Disable();
            }
        }

        void OnJumpPerformed(InputAction.CallbackContext ctx) {
            if (_jumping || playerImage == null)
                return;

            _co = StartCoroutine(JumpRoutine());
        }

        IEnumerator JumpRoutine() {
            _jumping = true;

            float dur = Mathf.Max(0.05f, jumpDuration);
            float edge = Mathf.Clamp(invulnEdgeTime, 0f, dur * 0.5f);

            if (!_lightCached)
                CacheLightBase();

            float t = 0f;

            while (t < dur) {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);

                float y = 4f * jumpHeight * u * (1f - u);
                playerImage.localPosition = _imgBaseLocalPos + new Vector3(0f, y, 0f);

                // 阴影缩放
                float h01 = 0f;
                if (shadow != null) {
                    h01 = Mathf.Clamp01(y / Mathf.Max(0.0001f, jumpHeight));
                    float multiplier = Mathf.Lerp(1f, shadowScaleMultiplierAtApex, h01);

                    shadow.localPosition = _shadowBaseLocalPos;
                    shadow.localScale = new Vector3(
                        _shadowBaseScale.x * multiplier,
                        _shadowBaseScale.y * multiplier,
                        _shadowBaseScale.z
                    );
                } else {
                    h01 = Mathf.Clamp01(y / Mathf.Max(0.0001f, jumpHeight));
                }

                // ✅ Light：随高度叠加强度 & 范围
                if (playerLight != null && _lightCached) {
                    float k = Mathf.Pow(h01, Mathf.Max(0.0001f, lightResponse)); // 可调“响应”
                    playerLight.intensity = _lightBaseIntensity + lightIntensityAddAtApex * k;
                    playerLight.pointLightOuterRadius = _lightBaseOuterRadius + lightRadiusAddAtApex * k;
                }

                bool edgeWindow = (t <= edge) || (t >= dur - edge);
                if (playerCollider != null)
                    playerCollider.enabled = edgeWindow;

                yield return null;
            }

            // 结束：复位
            playerImage.localPosition = _imgBaseLocalPos;

            if (shadow != null) {
                shadow.localPosition = _shadowBaseLocalPos;
                shadow.localScale = _shadowBaseScale;
            }

            if (playerLight != null && _lightCached) {
                playerLight.intensity = _lightBaseIntensity;
                playerLight.pointLightOuterRadius = _lightBaseOuterRadius;
            }

            if (playerCollider != null)
                playerCollider.enabled = true;

            _jumping = false;
            _co = null;
        }
    }
}