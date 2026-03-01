using System;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Arena;
using Gameplay.Pooling;

namespace Gameplay.Units {
    public class EnemySpawner : MonoBehaviour {

        [Serializable]
        public class EnemyType {
            public string name = "Type";

            [Header("Pooling")]
            public string poolKey = "EnemyA"; // PoolService entries 的 key

            [Header("Schedule")]
            public float startAfter = 0f;   // 开局延迟多少秒才开始刷
            public float interval = 1f;     // 刷怪间隔
            public int burst = 1;           // 一次刷几个（基础值）

            [Header("Burst Growth")]
            public float burstGrowEvery = 10f; // 每隔多少秒增长一次（从开始刷起算）
            public int burstGrowBy = 1;        // 每次增长多少
            public int burstMax = 20;          // burst 上限

            [NonSerialized]
            public float nextTime; // 运行时用
        }

        [Header("Refs")]
        public ArenaController arena;
        public Transform target;         // 玩家
        public PoolService pool;         // 拖 PoolService
        public Transform runtimeParent;  // Runtime（可空；不填就用 pool.runtimeParent）

        [Header("Enemies")]
        public List<EnemyType> types = new();

        [Header("Limits")]
        public int maxAliveTotal = 60;

        [Header("Spawn Ring (outside arena)")]
        public float minOffset = 2f;
        public float maxOffset = 6f;

        readonly List<Enemy> _alive = new();
        float _startTime;

        void Start() {
            _startTime = Time.time;

            for (int i = 0; i < types.Count; i++) {
                var t = types[i];
                if (t == null)
                    continue;

                t.nextTime = _startTime + Mathf.Max(0f, t.startAfter);
            }
        }

        void Update() {
            if (arena == null || target == null || pool == null || types == null || types.Count == 0)
                return;

            // 清理存活列表（池化时：被 Despawn 的对象会 inactive）
            for (int i = _alive.Count - 1; i >= 0; i--) {
                if (_alive[i] == null || !_alive[i].gameObject.activeInHierarchy)
                    _alive.RemoveAt(i);
            }

            if (_alive.Count >= maxAliveTotal)
                return;

            float now = Time.time;

            // 收集“到点要刷”的类型（✅ 不再做权重选择）
            List<int> due = null;

            for (int i = 0; i < types.Count; i++) {
                var t = types[i];
                if (t == null)
                    continue;
                if (string.IsNullOrEmpty(t.poolKey))
                    continue;

                if (now >= t.nextTime) {
                    due ??= new List<int>(4);
                    due.Add(i);
                }
            }

            if (due == null || due.Count == 0)
                return;

            // ✅ 所有到点的类型都刷
            for (int d = 0; d < due.Count; d++) {
                int idx = due[d];
                var type = types[idx];

                if (_alive.Count >= maxAliveTotal)
                    break;

                int burstNow = GetBurstNow(type, now);
                int canSpawn = Mathf.Min(burstNow, maxAliveTotal - _alive.Count);

                for (int k = 0; k < canSpawn; k++)
                    SpawnOne(type);

                // 推进该类型的下次时间（防止 interval=0 导致狂刷）
                float itv = Mathf.Max(0.01f, type.interval);
                type.nextTime = now + itv;
            }
        }

        int GetBurstNow(EnemyType type, float now) {
            int b = Mathf.Max(0, type.burst);
            float growEvery = type.burstGrowEvery;
            int growBy = type.burstGrowBy;

            if (growEvery > 0.01f && growBy != 0) {
                float t0 = _startTime + Mathf.Max(0f, type.startAfter); // 从开始刷起算
                float dt = now - t0;

                if (dt > 0f) {
                    int steps = Mathf.FloorToInt(dt / growEvery);
                    b += steps * growBy;
                }
            }

            int max = Mathf.Max(0, type.burstMax);
            b = Mathf.Clamp(b, 0, max);

            return b;
        }

        void SpawnOne(EnemyType type) {
            Vector2 pos = RandomPointInRing(
                arena.center,
                arena.radius + minOffset,
                arena.radius + maxOffset
            );

            var e = pool.Spawn<Enemy>(type.poolKey, pos, Quaternion.identity);
            if (e == null)
                return;

            if (runtimeParent != null)
                e.transform.SetParent(runtimeParent, false);

            e.Setup(pool);
            e.Init(target);

            _alive.Add(e);
        }

        static Vector2 RandomPointInRing(Vector2 center, float innerR, float outerR) {
            float r = Mathf.Sqrt(UnityEngine.Random.Range(innerR * innerR, outerR * outerR));
            float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
    }
}