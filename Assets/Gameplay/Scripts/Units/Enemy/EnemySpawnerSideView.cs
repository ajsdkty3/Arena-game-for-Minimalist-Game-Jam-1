/*using System;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Arena;
using Gameplay.Pooling;

namespace Gameplay.Units {
    public class EnemySpawnerSideView : MonoBehaviour {
        [Serializable]
        public class EnemyType {
            public string name = "Type";

            [Header("Pooling")]
            public string poolKey = "EnemyA";

            [Header("Schedule")]
            public float startAfter = 0f;
            public float interval = 1f;
            public int burst = 1;

            [Header("Burst Growth")]
            public float burstGrowEvery = 10f;
            public int burstGrowBy = 1;
            public int burstMax = 20;

            [Header("Pick Weight")]
            public int weight = 1;

            [NonSerialized] public float nextTime;
        }

        [Header("Refs")]
        public ArenaController arena;
        public Transform target;
        public PoolService pool;
        public Transform runtimeParent;

        [Header("Enemies")]
        public List<EnemyType> types = new();

        [Header("Limits")]
        public int maxAliveTotal = 60;

        [Header("Spawn Band (outside arena rect)")]
        public float minOffset = 2f;
        public float maxOffset = 6f;

        [Header("Spawn Height Fix")]
        public float minY = -2f;          // 如果生成点 y < minY
        public float liftIfBelow = 6f;    // 就把 y += liftIfBelow
        public float maxSpawnY = 999f;    // 可选上限
        public int maxTries = 30;

        [Header("Optional: avoid ground overlap (recommended)")]
        public LayerMask blockMask;        // 勾 Ground/Platform/Wall 等你不想生成进去的
        public float clearanceRadius = 0.35f;

        readonly List<Enemy> _alive = new();
        float _startTime;

        void Start() {
            _startTime = Time.time;

            for (int i = 0; i < types.Count; i++) {
                var t = types[i];
                t.nextTime = _startTime + Mathf.Max(0f, t.startAfter);
            }
        }

        void Update() {
            if (arena == null || target == null || pool == null || types == null || types.Count == 0)
                return;

            for (int i = _alive.Count - 1; i >= 0; i--) {
                if (_alive[i] == null || !_alive[i].gameObject.activeInHierarchy)
                    _alive.RemoveAt(i);
            }

            if (_alive.Count >= maxAliveTotal)
                return;

            float now = Time.time;

            List<int> due = null;
            int totalWeight = 0;

            for (int i = 0; i < types.Count; i++) {
                var t = types[i];
                if (t == null)
                    continue;
                if (string.IsNullOrEmpty(t.poolKey))
                    continue;

                if (now >= t.nextTime) {
                    due ??= new List<int>(4);
                    due.Add(i);
                    totalWeight += Mathf.Max(0, t.weight);
                }
            }

            if (due == null || due.Count == 0)
                return;

            int pickIndexInTypes = PickWeightedTypeIndex(due, totalWeight);
            var type = types[pickIndexInTypes];

            int burstNow = GetBurstNow(type, now);
            int canSpawn = Mathf.Min(burstNow, maxAliveTotal - _alive.Count);

            for (int k = 0; k < canSpawn; k++)
                SpawnOne(type);

            float itv = Mathf.Max(0.01f, type.interval);
            type.nextTime = now + itv;
        }

        int PickWeightedTypeIndex(List<int> due, int totalWeight) {
            if (totalWeight <= 0)
                return due[UnityEngine.Random.Range(0, due.Count)];

            int r = UnityEngine.Random.Range(1, totalWeight + 1);
            int acc = 0;

            for (int j = 0; j < due.Count; j++) {
                int idx = due[j];
                acc += Mathf.Max(0, types[idx].weight);
                if (r <= acc)
                    return idx;
            }

            return due[0];
        }

        int GetBurstNow(EnemyType type, float now) {
            int b = Mathf.Max(1, type.burst);

            float growEvery = type.burstGrowEvery;
            int growBy = type.burstGrowBy;

            if (growEvery > 0.01f && growBy != 0) {
                float t0 = _startTime + Mathf.Max(0f, type.startAfter);
                float dt = now - t0;
                if (dt > 0f) {
                    int steps = Mathf.FloorToInt(dt / growEvery);
                    b += steps * growBy;
                }
            }

            int max = Mathf.Max(1, type.burstMax);
            b = Mathf.Clamp(b, 1, max);
            return b;
        }

        void SpawnOne(EnemyType type) {
            Vector2 pos;
            if (!TryPickSpawnPos(out pos))
                return;

            var e = pool.Spawn<Enemy>(type.poolKey, pos, Quaternion.identity);
            if (e == null)
                return;

            if (runtimeParent != null)
                e.transform.SetParent(runtimeParent, false);

            e.Setup(pool);
            e.Init(target);

            _alive.Add(e);
        }

        bool TryPickSpawnPos(out Vector2 pos) {
            pos = default;

            int tries = Mathf.Max(1, maxTries);

            // arena 左右边界（考虑 padding：这里用 minOffset 把“边界”往外推一点也行）
            float halfW = arena.size.x * 0.5f;
            float leftX = arena.center.x - halfW;
            float rightX = arena.center.x + halfW;

            for (int i = 0; i < tries; i++) {
                Vector2 p = RandomPointOutsideRectBand(arena.center, arena.size, minOffset, maxOffset);

                // 低于阈值：抬高 + 把 x 贴到左右边框
                if (p.y < minY) {
                    p.y += liftIfBelow;
                    p.x = SnapXToArenaSide(p.x, leftX, rightX);
                }

                // 可选：上限
                if (p.y > maxSpawnY)
                    continue;

                // 可选：避免生成在地面/平台/墙里面
                if (blockMask.value != 0) {
                    float r = Mathf.Max(0.01f, clearanceRadius);
                    if (Physics2D.OverlapCircle(p, r, blockMask) != null)
                        continue;
                }

                pos = p;
                return true;
            }

            return false;
        }

        static float SnapXToArenaSide(float x, float leftX, float rightX) {
            // 哪边更近就贴哪边
            float dl = Mathf.Abs(x - leftX);
            float dr = Mathf.Abs(x - rightX);
            return dl <= dr ? leftX : rightX;
        }

        static Vector2 RandomPointOutsideRectBand(Vector2 center, Vector2 size, float minOffset, float maxOffset) {
            float minO = Mathf.Max(0f, minOffset);
            float maxO = Mathf.Max(minO + 0.0001f, maxOffset);

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            float inW = halfW + minO;
            float inH = halfH + minO;

            float outW = halfW + maxO;
            float outH = halfH + maxO;

            float topArea = (outW * 2f) * (outH - inH);
            float bottomArea = topArea;
            float rightArea = (outH * 2f) * (outW - inW);
            float leftArea = rightArea;

            float total = topArea + bottomArea + rightArea + leftArea;
            float r = UnityEngine.Random.Range(0f, total);

            if (r < topArea) {
                float x = UnityEngine.Random.Range(-outW, outW);
                float y = UnityEngine.Random.Range(inH, outH);
                return center + new Vector2(x, y);
            }
            r -= topArea;

            if (r < bottomArea) {
                float x = UnityEngine.Random.Range(-outW, outW);
                float y = UnityEngine.Random.Range(-outH, -inH);
                return center + new Vector2(x, y);
            }
            r -= bottomArea;

            if (r < rightArea) {
                float x = UnityEngine.Random.Range(inW, outW);
                float y = UnityEngine.Random.Range(-outH, outH);
                return center + new Vector2(x, y);
            }

            float lx = UnityEngine.Random.Range(-outW, -inW);
            float ly = UnityEngine.Random.Range(-outH, outH);
            return center + new Vector2(lx, ly);
        }
    }
}
*/