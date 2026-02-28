using UnityEngine;

namespace Gameplay.Arena {
    public class ArenaControllerSideView : MonoBehaviour {
        [Header("Rect Arena (world space)")]
        public Vector2 center = Vector2.zero;
        public Vector2 size = new Vector2(16f, 9f); // 宽高

        void OnValidate() {
            if (size.x < 0f)
                size.x = 0f;
            if (size.y < 0f)
                size.y = 0f;
        }

        public Vector2 ClampInside(Vector2 pos, float padding = 0f) {
            float halfW = Mathf.Max(0f, size.x * 0.5f - padding);
            float halfH = Mathf.Max(0f, size.y * 0.5f - padding);

            float minX = center.x - halfW;
            float maxX = center.x + halfW;
            float minY = center.y - halfH;
            float maxY = center.y + halfH;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            return pos;
        }

        void OnDrawGizmosSelected() {
            Vector3 c = new Vector3(center.x, center.y, 0f);
            Vector3 s = new Vector3(size.x, size.y, 0f);
            Gizmos.DrawWireCube(c, s);
        }
    }
}