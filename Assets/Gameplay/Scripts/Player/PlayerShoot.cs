using UnityEngine;
using UnityEngine.InputSystem;
using Gameplay.Pooling;
using Gameplay.Projectiles;

namespace Gameplay.Player {
    public class PlayerShoot : MonoBehaviour {
        [Header("Input")]
        public InputActionReference fire;

        [Header("Pooling")]
        public PoolService pool;
        public string bulletKey = "Bullet";

        [Header("Fire")]
        public Transform muzzle;
        public float fireInterval = 0.25f; // 初始间隔（越小越快）

        [Header("Fire Ramp (step based)")]
        public bool rampFireRate = true;
        public float intervalStepSeconds = 10f;   // 每多少秒提升一次射速
        [Range(0.5f, 1f)]
        public float fireMultiplier = 0.9f;       // 每次提升：间隔 *= 0.9（更快）
        public float minFireInterval = 0.08f;     // 最快间隔下限
        public bool useUnscaledTime = false;      // 如果你会 Time.timeScale=0（暂停/HitStop）可勾

        [Header("Audio")]
        public AudioSource shootAudioSource;   // 拖一个AudioSource
        public AudioClip shootClip;
        [Range(0.8f, 1.2f)] public float pitchMin = 0.95f;
        [Range(0.8f, 1.2f)] public float pitchMax = 1.05f;

        Camera _cam;
        float _cd;

        float _currentFireInterval;
        float _nextRampTime;

        float Now() => useUnscaledTime ? Time.unscaledTime : Time.time;

        void Awake() {
            _cam = Camera.main;

            _currentFireInterval = fireInterval;
            _nextRampTime = Now() + Mathf.Max(0.01f, intervalStepSeconds);
        }

        void OnEnable() { if (fire != null) fire.action.Enable(); }
        void OnDisable() { if (fire != null) fire.action.Disable(); }

        void Update() {
            // step-based ramp
            if (rampFireRate) {
                float step = Mathf.Max(0.01f, intervalStepSeconds);
                float now = Now();

                // 防止卡顿/切回窗口导致一次跳过很多秒：用 while 追上进度
                while (now >= _nextRampTime && _currentFireInterval > minFireInterval) {
                    _currentFireInterval *= fireMultiplier;
                    if (_currentFireInterval < minFireInterval)
                        _currentFireInterval = minFireInterval;

                    _nextRampTime += step;
                }
            }

            _cd -= Time.deltaTime;

            if (pool == null || _cam == null)
                return;
            if (_cd > 0f)
                return;
            if (fire == null || !fire.action.IsPressed())
                return;

            Vector2 origin = muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;

            var ms = Mouse.current.position.ReadValue();
            var mw3 = _cam.ScreenToWorldPoint(new Vector3(ms.x, ms.y, -_cam.transform.position.z));
            Vector2 mouseWorld = (Vector2)mw3;

            Vector2 dir = mouseWorld - origin;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;

            var b = pool.Spawn<Bullet>(bulletKey, origin, Quaternion.identity);
            b.Setup(pool);
            b.Fire(dir);

            PlayShootSound();

            _cd = _currentFireInterval;
        }

        void PlayShootSound() {
            if (shootAudioSource == null || shootClip == null)
                return;

            shootAudioSource.pitch = Random.Range(pitchMin, pitchMax);
            shootAudioSource.PlayOneShot(shootClip);
        }
    }
}