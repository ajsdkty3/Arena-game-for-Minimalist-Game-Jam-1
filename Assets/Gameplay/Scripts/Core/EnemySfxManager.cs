using System.Collections.Generic;
using UnityEngine;

public class EnemySfxManager : MonoBehaviour {
    public static EnemySfxManager I { get; private set; }

    [Header("Global Rate Limit")]
    [Tooltip("全局每秒最多允许播放多少次敌人音效（防止一窝蜂吼叫）")]
    public float maxPlaysPerSecond = 8f;

    [Tooltip("同一敌人最短间隔（兜底，防止单个敌人刷屏）")]
    public float perEnemyCooldown = 0.25f;

    [Header("Optional: Nearest-Only Gate")]
    [Tooltip("开启后：同一帧/同一窗口，只允许“最近的若干个”敌人播（更干净）")]
    public bool enableNearestGate = false;

    [Tooltip("最近允许播放的敌人数")]
    public int nearestAllowCount = 6;

    [Tooltip("Nearest Gate 的刷新窗口（秒）。越小越灵敏，越大越稳定。")]
    public float nearestWindow = 0.15f;

    float _nextGlobalTime;
    readonly Dictionary<int, float> _nextEnemyTime = new();

    // nearest gate
    float _nextNearestRebuildTime;
    readonly List<Entry> _entries = new();
    readonly HashSet<int> _allowedNearest = new();

    struct Entry {
        public int enemyId;
        public float distSq;
    }

    void Awake() {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 尝试播放敌人音效。可传 playerPos/thisPos 让 nearest gate 工作。
    /// </summary>
    public bool TryPlay(AudioSource src, AudioClip clip, int enemyId, float volume,
                        bool useNearestGate, Vector2 playerPos, Vector2 enemyPos) {
        if (src == null || clip == null)
            return false;

        float now = Time.time;

        // 全局限流
        float globalInterval = maxPlaysPerSecond <= 0f ? 0f : (1f / maxPlaysPerSecond);
        if (now < _nextGlobalTime)
            return false;

        // 单体限流
        if (_nextEnemyTime.TryGetValue(enemyId, out float t) && now < t)
            return false;

        // 最近优先 gate（可选）
        if (enableNearestGate && useNearestGate) {
            RebuildNearestAllowedIfNeeded(now);
            RegisterDistance(enemyId, playerPos, enemyPos);

            // 这里再 rebuild 一次，确保本帧有数据
            RebuildNearestAllowedIfNeeded(now, force: true);

            if (!_allowedNearest.Contains(enemyId))
                return false;
        }

        src.PlayOneShot(clip, volume);

        _nextGlobalTime = now + globalInterval;
        _nextEnemyTime[enemyId] = now + perEnemyCooldown;

        return true;
    }

    void RegisterDistance(int enemyId, Vector2 playerPos, Vector2 enemyPos) {
        float dsq = (enemyPos - playerPos).sqrMagnitude;
        _entries.Add(new Entry { enemyId = enemyId, distSq = dsq });
    }

    void RebuildNearestAllowedIfNeeded(float now, bool force = false) {
        if (!force && now < _nextNearestRebuildTime)
            return;

        _nextNearestRebuildTime = now + Mathf.Max(0.02f, nearestWindow);

        _allowedNearest.Clear();

        if (_entries.Count == 0)
            return;

        // 取最近 N 个
        _entries.Sort((a, b) => a.distSq.CompareTo(b.distSq));

        int n = Mathf.Clamp(nearestAllowCount, 1, 9999);
        int take = Mathf.Min(n, _entries.Count);

        for (int i = 0; i < take; i++)
            _allowedNearest.Add(_entries[i].enemyId);

        _entries.Clear();
    }
}