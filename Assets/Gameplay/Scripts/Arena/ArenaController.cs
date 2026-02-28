using UnityEngine;

namespace Gameplay.Arena {
    public class ArenaController : MonoBehaviour {
        public Vector2 center = new Vector2(0f, 0f);
        public float radius = 8f;

        void OnValidate() {
            if (radius < 0f)
                radius = 0f;
        }

        public Vector2 ClampInside(Vector2 pos, float padding = 0f) {
            var c = center;
            var maxR = Mathf.Max(0f, radius - padding);
            var v = pos - c;
            var d = v.magnitude;

            if (d <= maxR || d <= 0.0001f)
                return pos;

            return c + v / d * maxR;
        }

        void OnDrawGizmosSelected() {
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}