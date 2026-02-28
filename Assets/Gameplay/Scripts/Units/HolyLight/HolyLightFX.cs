using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gameplay.VFX {
    public class HolyLightFX : MonoBehaviour {

        [Header("Lights")]
        public Light2D holyLightAura;
        public Light2D mainLight;

        [Header("Damage Model")]
        public int hitsToZero = 5;
        [Range(0f, 1f)] public float damagePercentOfMax = 0.2f; // hitsToZero<=0 ??
        public float minIntensity = 0f;

        [Header("Hit Flash")]
        public float fadeDownTime = 0.2f;
        public float recoverTime = 0.3f;

        [Header("Hit Shiver (scared shake)")]
        public bool enableShiver = true;
        public float shiverDuration = 0.18f;
        public float shiverFrequency = 28f;
        public float shiverPosAmount = 0.05f;
        public float shiverScaleAmount = 0.06f;
        public Transform shiverRoot;

        [Header("Mask Fade")]
        public SpriteRenderer maskRenderer;
        [Range(0f, 1f)] public float maskAlphaStep = 0.2f;

        [Header("Debug")]
        public bool debugLog = false;

        float _auraMax, _mainMax;
        float _auraTarget, _mainTarget;
        float _auraStart, _mainStart;

        float _flashT;
        bool _flashing;

        float _shiverT;
        bool _shivering;
        Vector3 _basePos;
        Vector3 _baseScale;

        int _hitCount;
        bool _dead;

        public event Action OnFlashBegin;
        public event Action OnFlashEnd;
        public event Action OnDead;

        public int HitCount => _hitCount;
        public bool IsDead => _dead;
        public bool IsFlashing => _flashing;

        void Awake() {
            CaptureMaxIfNeeded(force: true);

            if (shiverRoot == null)
                shiverRoot = transform;

            _basePos = shiverRoot.localPosition;
            _baseScale = shiverRoot.localScale;
        }

        void Update() {
            if (_dead) {
                LockToMin();
                return;
            }

            if (_flashing)
                TickFlash(Time.deltaTime);

            if (_shivering)
                TickShiver(Time.deltaTime);
        }

        void CaptureMaxIfNeeded(bool force = false) {
            if (holyLightAura != null && (force || _auraMax <= 0f)) {
                _auraMax = holyLightAura.intensity;
                _auraTarget = _auraMax;
            }
            if (mainLight != null && (force || _mainMax <= 0f)) {
                _mainMax = mainLight.intensity;
                _mainTarget = _mainMax;
            }

            if (debugLog)
                Debug.Log($"[HolyLightFX] Max captured: auraMax={_auraMax:F3}, mainMax={_mainMax:F3}", this);
        }

        float CalcStep(float max) {
            if (max <= 0f)
                return 0f;

            if (hitsToZero > 0)
                return max / hitsToZero;

            return max * damagePercentOfMax;
        }

        public void PlayHitFX() {
            if (_dead)
                return;

            _hitCount++;
            CaptureMaxIfNeeded();

            _auraStart = holyLightAura != null ? holyLightAura.intensity : 0f;
            _mainStart = mainLight != null ? mainLight.intensity : 0f;

            float auraStep = CalcStep(_auraMax);
            float mainStep = CalcStep(_mainMax);

            if (holyLightAura != null)
                _auraTarget = Mathf.Max(minIntensity, _auraTarget - auraStep);
            if (mainLight != null)
                _mainTarget = Mathf.Max(minIntensity, _mainTarget - mainStep);

            _flashing = true;
            _flashT = 0f;
            OnFlashBegin?.Invoke();

            if (enableShiver) {
                StartShiver();
                ApplyMaskFade();
            }

            if (debugLog)
                Debug.Log($"[HolyLightFX] HitCount={_hitCount}/{hitsToZero} targets aura={_auraTarget:F3} main={_mainTarget:F3}", this);
        }

        void ApplyMaskFade() {
            if (maskRenderer == null)
                return;

            var c = maskRenderer.color;
            c.a = Mathf.Clamp01(c.a + maskAlphaStep);
            maskRenderer.color = c;
        }

        void StartShiver() {
            if (shiverRoot == null)
                return;

            _basePos = shiverRoot.localPosition;
            _baseScale = shiverRoot.localScale;

            _shivering = true;
            _shiverT = 0f;
        }

        void TickShiver(float dt) {
            _shiverT += dt;

            float dur = Mathf.Max(0.0001f, shiverDuration);
            float k = Mathf.Clamp01(_shiverT / dur);
            float amp = 1f - k;

            float s1 = Mathf.Sin(_shiverT * shiverFrequency * Mathf.PI * 2f);
            float s2 = Mathf.Sin((_shiverT * shiverFrequency * 1.37f) * Mathf.PI * 2f);

            Vector3 off = new Vector3(s1, s2, 0f) * (shiverPosAmount * amp);
            shiverRoot.localPosition = _basePos + off;

            float squash = s1 * (shiverScaleAmount * amp);
            float xMul = 1f + squash;
            float yMul = 1f - squash;

            shiverRoot.localScale = new Vector3(_baseScale.x * xMul, _baseScale.y * yMul, _baseScale.z);

            if (_shiverT >= dur) {
                shiverRoot.localPosition = _basePos;
                shiverRoot.localScale = _baseScale;
                _shivering = false;
            }
        }

        void TickFlash(float dt) {
            _flashT += dt;

            float down = Mathf.Max(0.0001f, fadeDownTime);
            float up = Mathf.Max(0.0001f, recoverTime);
            float total = down + up;

            if (_flashT <= down) {
                float k = Mathf.Clamp01(_flashT / down);
                if (holyLightAura != null)
                    holyLightAura.intensity = Mathf.Lerp(_auraStart, 0f, k);
                if (mainLight != null)
                    mainLight.intensity = Mathf.Lerp(_mainStart, 0f, k);
            } else {
                float t2 = _flashT - down;
                float k = Mathf.Clamp01(t2 / up);
                if (holyLightAura != null)
                    holyLightAura.intensity = Mathf.Lerp(0f, _auraTarget, k);
                if (mainLight != null)
                    mainLight.intensity = Mathf.Lerp(0f, _mainTarget, k);
            }

            if (_flashT >= total) {
                if (holyLightAura != null)
                    holyLightAura.intensity = _auraTarget;
                if (mainLight != null)
                    mainLight.intensity = _mainTarget;

                _flashing = false;
                OnFlashEnd?.Invoke();

                // ? dead ???? hitCount
                if (hitsToZero > 0 && _hitCount >= hitsToZero) {
                    EnterDead();
                }
            }
        }

        void EnterDead() {
            if (_dead)
                return;

            _dead = true;

            // ? ????????? minIntensity????????“???”
            _auraTarget = minIntensity;
            _mainTarget = minIntensity;
            _auraStart = minIntensity;
            _mainStart = minIntensity;

            _flashing = false;
            _flashT = 0f;

            LockToMin();

            if (shiverRoot == null)
                shiverRoot = transform;
            shiverRoot.localPosition = _basePos;
            shiverRoot.localScale = _baseScale;
            _shivering = false;
            _shiverT = 0f;

            if (debugLog)
                Debug.Log("[HolyLightFX] EnterDead()", this);

            OnDead?.Invoke();
        }

        void LockToMin() {
            if (holyLightAura != null)
                holyLightAura.intensity = minIntensity;
            if (mainLight != null)
                mainLight.intensity = minIntensity;
        }

        [ContextMenu("Reset FX")]
        public void ResetFX() {
            CaptureMaxIfNeeded(force: true);

            _hitCount = 0;
            _dead = false;

            _flashing = false;
            _flashT = 0f;

            if (holyLightAura != null)
                holyLightAura.intensity = _auraTarget;
            if (mainLight != null)
                mainLight.intensity = _mainTarget;

            if (shiverRoot == null)
                shiverRoot = transform;
            shiverRoot.localPosition = _basePos;
            shiverRoot.localScale = _baseScale;
            _shivering = false;
            _shiverT = 0f;

            if (maskRenderer != null) {
                var c = maskRenderer.color;
                c.a = 0f;
                maskRenderer.color = c;
            }
        }
    }
}