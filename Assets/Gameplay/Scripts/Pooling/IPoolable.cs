namespace Gameplay.Pooling {
    public interface IPoolable {
        void OnSpawn();
        void OnDespawn();
    }
}