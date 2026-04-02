using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct ShootToData : IComponentData
    {
        public float Every;
        public float ReadyRemaining;
    }
}
