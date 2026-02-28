using UnityEngine;

namespace Gameplay.Units.Movement {
    public interface IEnemyMovement {
        void Tick(Transform self, Transform target, float dt);
        void ResetState();
    }
}