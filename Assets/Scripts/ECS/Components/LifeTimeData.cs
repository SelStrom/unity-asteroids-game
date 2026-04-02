using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct LifeTimeData : IComponentData
    {
        public float TimeRemaining;
    }
}
