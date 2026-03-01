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
        public float fireInterval = 0.12f;

        [Header("Audio")]
        public AudioSource shootAudioSource;   // 拖一个AudioSource
        public AudioClip shootClip;
        [Range(0.8f, 1.2f)] public float pitchMin = 0.95f;
        [Range(0.8f, 1.2f)] public float pitchMax = 1.05f;

        Camera _cam;
        float _cd;

        void Awake() => _cam = Camera.main;

        void OnEnable() { if (fire != null) fire.action.Enable(); }
        void OnDisable() { if (fire != null) fire.action.Disable(); }

        void Update() {
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

            _cd = fireInterval;
        }

        void PlayShootSound() {
            if (shootAudioSource == null || shootClip == null)
                return;

            shootAudioSource.pitch = Random.Range(pitchMin, pitchMax);
            shootAudioSource.PlayOneShot(shootClip);
        }
    }
}