using UnityEngine;

namespace Gameplay.VFX {
    public class SlimeSquashIdle : MonoBehaviour {
        [Header("Pivot (bottom stays fixed)")]
        public Transform bottomPivot; // 放一个子物体在球的最底部，当锚点

        [Header("Motion")]
        [Min(0.01f)] public float speed = 1.2f;          // 动画速度（越大越快）
        [Range(0f, 0.5f)] public float amount = 0.08f;   // 幅度（默认很小）
        [Range(0f, 1f)] public float xWeight = 1.0f;     // X变化权重
        [Range(0f, 1f)] public float yWeight = 1.0f;     // Y变化权重

        [Header("Scale Base")]
        public Vector3 baseScale = Vector3.one;          // 默认缩放（可选）
        public bool autoCaptureBaseScale = true;

        Vector3 _baseScaleRuntime;
        float _t;

        void Awake() {
            if (autoCaptureBaseScale)
                _baseScaleRuntime = transform.localScale;
            else
                _baseScaleRuntime = baseScale;

            if (bottomPivot == null) {
                Debug.LogWarning($"{name}: SlimeSquashIdle 缺少 bottomPivot（底部锚点），将无法保持底部不动。");
            }
        }

        void LateUpdate() {
            _t += Time.deltaTime;

            // -1..1 循环
            float s = Mathf.Sin(_t * speed * Mathf.PI * 2f);

            // X宽时Y矮：X = +, Y = -
            float xMul = 1f + (s * amount * xWeight);
            float yMul = 1f - (s * amount * yWeight);

            Vector3 newScale = _baseScaleRuntime;
            newScale.x *= xMul;
            newScale.y *= yMul;

            // 你是2D的话Z通常保持不变；3D也可以一起轻微补偿
            newScale.z = _baseScaleRuntime.z;

            // 缩放前后都把“底部世界坐标”对齐回去
            Vector3 bottomWorldBefore = bottomPivot ? bottomPivot.position : transform.position;

            transform.localScale = newScale;

            if (bottomPivot) {
                Vector3 bottomWorldAfter = bottomPivot.position;
                Vector3 delta = bottomWorldBefore - bottomWorldAfter;
                transform.position += delta;
            }
        }

        // 方便在Inspector点一下就重抓当前缩放当base
        [ContextMenu("Capture Base Scale Now")]
        void CaptureBaseScaleNow() {
            _baseScaleRuntime = transform.localScale;
        }
    }
}