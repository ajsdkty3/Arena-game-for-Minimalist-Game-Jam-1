// HolyLightDamage.cs
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Gameplay.VFX {
    public class HolyLightDamage : MonoBehaviour {

        [Header("Lights")]
        public Light2D holyLightAura;
        public Light2D mainLight;

        [Header("Damage")]
        [Range(0f, 1f)] public float damagePercentOfMax = 0.2f;
        public int hitsToZero = 5;
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

        [Tooltip("不想抖整个 HolyLight 根物体，就指定一个子物体来抖")]
        public Transform shiverRoot;

        [Header("Mask Fade")]
        public SpriteRenderer maskRenderer;
        [Range(0f, 1f)] public float maskAlphaStep = 0.2f;

        [Header("Player Trigger Control")]
        [Tooltip("拖 Player 的 Collider2D（需要在熄灭前把 isTrigger 关掉）")]
        public Collider2D playerColliderToDisableTrigger;

        [Tooltip("当灯强度低于这个阈值，就把 playerCollider.isTrigger = false")]
        public float triggerOffWhenBelow = 0.05f;

        [Header("Dead Behavior")]
        [Tooltip("到 0 后不再 SetActive(false)，而是保持存在并锁定强度")]
        public bool keepAliveWhenDead = true;

        [Header("Debug")]
        public bool debugLog = false;

        float _auraMax, _mainMax;
        float _auraTarget, _mainTarget;

        float _auraStart, _mainStart;
        float _flashT;
        bool _flashing;

        // ---- shiver runtime ----
        float _shiverT;
        bool _shivering;
        Vector3 _basePos;
        Vector3 _baseScale;

        int _hitCount;

        // dead state (fully off)
        bool _dead;

        // ✅ events for external controllers (like Flicker)
        public event Action OnFlashBegin;
        public event Action OnFlashEnd;
        public event Action OnDead; // optional: 熄灭那一刻通知

        public float AuraBase => _auraTarget;
        public float MainBase => _mainTarget;
        public bool IsFlashing => _flashing;
        public bool IsDead => _dead;

        void Awake() {
            CaptureMaxIfNeeded(force: true);

            if (shiverRoot == null)
                shiverRoot = transform;

            _basePos = shiverRoot.localPosition;
            _baseScale = shiverRoot.localScale;
        }

        void Update() {
            if (_dead) {
                // 锁定到最小强度（通常 0）
                if (holyLightAura != null)
                    holyLightAura.intensity = minIntensity;
                if (mainLight != null)
                    mainLight.intensity = minIntensity;
                return;
            }

            if (_flashing)
                TickFlash(Time.deltaTime);

            if (_shivering)
                TickShiver(Time.deltaTime);

            // 熄灭过程中：提前关掉 player isTrigger（只要低于阈值就关）
            TryDisablePlayerTriggerByCurrentIntensity();
        }

        void OnDisable() {
            // pooling / disable 时清状态（不强制改 player trigger，避免副作用）
            _flashing = false;
            _shivering = false;
            _flashT = 0f;
            _shiverT = 0f;
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
                Debug.Log($"[HolyLightDamage] Max captured: auraMax={_auraMax:F3}, mainMax={_mainMax:F3}", this);
        }

        public void Hit() {
            if (_dead)
                return;

            _hitCount++;

            CaptureMaxIfNeeded();

            _auraStart = holyLightAura != null ? holyLightAura.intensity : 0f;
            _mainStart = mainLight != null ? mainLight.intensity : 0f;

            float auraStep = CalcStep(_auraMax);
            float mainStep = CalcStep(_mainMax);

            if (holyLightAura != null) {
                float before = _auraTarget;
                _auraTarget = Mathf.Max(minIntensity, _auraTarget - auraStep);
                if (debugLog)
                    Debug.Log($"[HolyLightDamage] HIT aura: start={_auraStart:F3} target {before:F3}->{_auraTarget:F3}", this);
            }

            if (mainLight != null) {
                float before = _mainTarget;
                _mainTarget = Mathf.Max(minIntensity, _mainTarget - mainStep);
                if (debugLog)
                    Debug.Log($"[HolyLightDamage] HIT main: start={_mainStart:F3} target {before:F3}->{_mainTarget:F3}", this);
            }

            _flashing = true;
            _flashT = 0f;
            OnFlashBegin?.Invoke();

            if (enableShiver) {
                StartShiver();
                ApplyMaskFade();
            }

            // 达到熄灭条件：进入 dead（但不 disable 自己）
            if (hitsToZero > 0 && _hitCount >= hitsToZero) {
                if (debugLog)
                    Debug.Log("[HolyLightDamage] hitsToZero reached -> DEAD", this);

                // 注意：不要立刻把 _dead=true，否则本次 flash 没机会播完
                // 所以这里标记目标值已经是 minIntensity，等 flash 结束时再进入 dead
            }
        }

        void ApplyMaskFade() {
            if (maskRenderer == null)
                return;

            Color c = maskRenderer.color;
            c.a = Mathf.Clamp01(c.a + maskAlphaStep);
            maskRenderer.color = c;

            if (debugLog)
                Debug.Log($"[HolyLightDamage] Mask alpha -> {c.a:F2}", this);
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

        float CalcStep(float max) {
            if (max <= 0f)
                return 0f;
            if (hitsToZero > 0)
                return max / hitsToZero;
            return max * damagePercentOfMax;
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

            // 熄灭过程中：也在 flash 里检测一次（更及时）
            TryDisablePlayerTriggerByCurrentIntensity();

            if (_flashT >= total) {
                if (holyLightAura != null)
                    holyLightAura.intensity = _auraTarget;
                if (mainLight != null)
                    mainLight.intensity = _mainTarget;

                _flashing = false;
                OnFlashEnd?.Invoke();

                // 如果已经到 hitsToZero，且 target 已经到 minIntensity（通常 0） -> 进入 dead
                if (hitsToZero > 0 && _hitCount >= hitsToZero &&
                    Mathf.Approximately(_auraTarget, minIntensity) &&
                    Mathf.Approximately(_mainTarget, minIntensity)) {

                    EnterDead();
                }
            }
        }

        void TryDisablePlayerTriggerByCurrentIntensity() {
            if (playerColliderToDisableTrigger == null)
                return;

            if (!playerColliderToDisableTrigger.isTrigger)
                return;

            float auraI = holyLightAura != null ? holyLightAura.intensity : 0f;
            float mainI = mainLight != null ? mainLight.intensity : 0f;
            float cur = Mathf.Max(auraI, mainI);

            if (cur <= triggerOffWhenBelow) {
                playerColliderToDisableTrigger.isTrigger = false;
                if (debugLog)
                    Debug.Log($"[HolyLightDamage] Player isTrigger -> false (curIntensity={cur:F3})", this);
            }
        }

        void EnterDead() {
            _dead = true;

            // 最终锁定
            if (holyLightAura != null)
                holyLightAura.intensity = minIntensity;
            if (mainLight != null)
                mainLight.intensity = minIntensity;

            // 确保抖动归位
            if (shiverRoot == null)
                shiverRoot = transform;
            shiverRoot.localPosition = _basePos;
            shiverRoot.localScale = _baseScale;
            _shivering = false;
            _shiverT = 0f;

            OnDead?.Invoke();

            if (debugLog)
                Debug.Log("[HolyLightDamage] EnterDead()", this);

            // 你说“完全熄灭后不用 disable 自己”
            if (!keepAliveWhenDead) {
                gameObject.SetActive(false);
            }
        }

        [ContextMenu("Reset To Max")]
        void ResetToMax() {
            CaptureMaxIfNeeded(force: true);

            _dead = false;

            _flashing = false;
            _flashT = 0f;

            if (holyLightAura != null)
                holyLightAura.intensity = _auraTarget;
            if (mainLight != null)
                mainLight.intensity = _mainTarget;

            _hitCount = 0;

            if (shiverRoot == null)
                shiverRoot = transform;
            shiverRoot.localPosition = _basePos;
            shiverRoot.localScale = _baseScale;
            _shivering = false;
            _shiverT = 0f;
        }
    }
}