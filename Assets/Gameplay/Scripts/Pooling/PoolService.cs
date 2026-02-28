using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Pooling {
    public class PoolService : MonoBehaviour {
        [Serializable]
        public class Entry {
            public string key;
            public GameObject prefab;
            public int warmup = 20;
        }

        [Header("Runtime parent (optional)")]
        public Transform runtimeParent;

        [Header("Register prefabs here")]
        public List<Entry> entries = new();

        readonly Dictionary<string, Queue<GameObject>> _inactive = new();
        readonly Dictionary<string, GameObject> _prefabs = new();

        void Awake() {
            RegisterAll(entries);

            // warmup
            for (int i = 0; i < entries.Count; i++) {
                var e = entries[i];
                if (!IsValidEntry(e))
                    continue;

                Warmup(e.key, e.warmup);
            }
        }

        bool IsValidEntry(Entry e)
            => e != null && !string.IsNullOrEmpty(e.key) && e.prefab != null;

        public void RegisterAll(List<Entry> list) {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++) {
                var e = list[i];
                if (!IsValidEntry(e))
                    continue;

                _prefabs[e.key] = e.prefab;

                if (!_inactive.ContainsKey(e.key))
                    _inactive[e.key] = new Queue<GameObject>();
            }
        }

        public void Warmup(string key, int count) {
            if (count <= 0)
                return;
            if (!_prefabs.TryGetValue(key, out var prefab)) {
                Debug.LogError($"[Pool] Warmup failed, missing key: {key}");
                return;
            }

            if (!_inactive.TryGetValue(key, out var q)) {
                q = new Queue<GameObject>();
                _inactive[key] = q;
            }

            for (int i = 0; i < count; i++) {
                var go = CreateInstance(prefab);
                PrepareDespawn(go, key);
                q.Enqueue(go);
            }
        }

        GameObject CreateInstance(GameObject prefab) {
            var go = Instantiate(prefab, runtimeParent);
            go.SetActive(false);

            // ensure it has PooledObject with correct key (fallback)
            var po = go.GetComponent<PooledObject>();
            if (po == null)
                po = go.AddComponent<PooledObject>();

            return go;
        }

        public GameObject Spawn(string key, Vector3 pos, Quaternion rot) {
            if (!_prefabs.TryGetValue(key, out var prefab)) {
                Debug.LogError($"[Pool] Spawn failed, missing key: {key}");
                return null;
            }

            if (!_inactive.TryGetValue(key, out var q)) {
                q = new Queue<GameObject>();
                _inactive[key] = q;
            }

            GameObject go = (q.Count > 0) ? q.Dequeue() : CreateInstance(prefab);

            // set key
            var po = go.GetComponent<PooledObject>();
            if (po == null)
                po = go.AddComponent<PooledObject>();
            po.key = key;

            // place
            go.transform.SetParent(runtimeParent, false);
            go.transform.SetPositionAndRotation(pos, rot);

            // enable + callbacks
            go.SetActive(true);
            CallOnSpawn(go);

            return go;
        }

        public T Spawn<T>(string key, Vector3 pos, Quaternion rot) where T : Component {
            var go = Spawn(key, pos, rot);
            return go != null ? go.GetComponent<T>() : null;
        }

        public void Despawn(GameObject go) {
            if (go == null)
                return;

            var po = go.GetComponent<PooledObject>();
            if (po == null || string.IsNullOrEmpty(po.key)) {
                // 不在池里：直接关掉/销毁都行，这里选择关掉
                go.SetActive(false);
                return;
            }

            Despawn(po.key, go);
        }

        public void Despawn(string key, GameObject go) {
            if (go == null)
                return;

            if (!_inactive.TryGetValue(key, out var q)) {
                q = new Queue<GameObject>();
                _inactive[key] = q;
            }

            CallOnDespawn(go);
            PrepareDespawn(go, key);
            q.Enqueue(go);
        }

        void PrepareDespawn(GameObject go, string key) {
            var po = go.GetComponent<PooledObject>();
            if (po == null)
                po = go.AddComponent<PooledObject>();
            po.key = key;

            go.SetActive(false);
            go.transform.SetParent(runtimeParent, false);
        }

        static void CallOnSpawn(GameObject go) {
            var list = ListCache<IPoolable>.Get();
            go.GetComponents(list);
            for (int i = 0; i < list.Count; i++)
                list[i].OnSpawn();
            list.Clear();
        }

        static void CallOnDespawn(GameObject go) {
            var list = ListCache<IPoolable>.Get();
            go.GetComponents(list);
            for (int i = 0; i < list.Count; i++)
                list[i].OnDespawn();
            list.Clear();
        }

        // tiny non-alloc cache
        static class ListCache<T> {
            static readonly List<T> _list = new(8);
            public static List<T> Get() => _list;
        }
    }
}