using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gameplay.VFX {
    [RequireComponent(typeof(Light2D))]
    public class FlickerFromHolyLightBase2D : MonoBehaviour {
        Light2D _l;

        [Header("HolyLightFX")]
        public HolyLightFX holyLight;   // ✅ 改成 HolyLightFX

        [Header("Flicker")]
        public float breatheSpeed = 0.5f;
        public float breatheAmount = 0.03f;
        public float flickerSpeed = 10f;
        public float flickerAmount = 0.06f;

        float _baseI;
        bool _suspended;

        void Awake() {
            _l = GetComponent<Light2D>();
            _baseI = _l.intensity;
        }

        void OnEnable() {
            if (holyLight != null) {
                holyLight.OnFlashBegin += HandleFlashBegin;
                holyLight.OnFlashEnd += HandleFlashEnd;
                holyLight.OnDead += HandleDead;

                _suspended = holyLight.IsFlashing || holyLight.IsDead;
            } else {
                _suspended = false;
            }

            _baseI = _l.intensity;
        }

        void OnDisable() {
            if (holyLight != null) {
                holyLight.OnFlashBegin -= HandleFlashBegin;
                holyLight.OnFlashEnd -= HandleFlashEnd;
                holyLight.OnDead -= HandleDead;
            }
        }

        void HandleFlashBegin() {
            _suspended = true;
        }

        void HandleFlashEnd() {
            // 从“当下 intensity”继续抖
            _baseI = _l.intensity;
            _suspended = false;
        }

        void HandleDead() {
            _suspended = true;
            // 可选：这里也强制一次归零，防止同帧被别的脚本写回
            _l.intensity = 0f;
        }

        void LateUpdate() {
            if (holyLight == null)
                return;

            // ✅ dead 以后永远不再写（否则会把 0 抬起来）
            if (holyLight.IsDead)
                return;

            // ✅ flash 期间让路
            if (_suspended || holyLight.IsFlashing)
                return;

            float t = Time.time;
            float breathe = Mathf.Sin(t * breatheSpeed * Mathf.PI * 2f) * breatheAmount;
            float n = (Mathf.PerlinNoise(t * flickerSpeed, 0.37f) - 0.5f) * 2f;
            float flicker = n * flickerAmount;

            _l.intensity = Mathf.Max(0f, _baseI + breathe + flicker);
        }
    }
}