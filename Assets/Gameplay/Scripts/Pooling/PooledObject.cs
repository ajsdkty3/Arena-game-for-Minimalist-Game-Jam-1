using UnityEngine;

namespace Gameplay.Pooling {
    public class PooledObject : MonoBehaviour {
        [Tooltip("Pool key must match PoolService entries key")]
        public string key;
    }
}