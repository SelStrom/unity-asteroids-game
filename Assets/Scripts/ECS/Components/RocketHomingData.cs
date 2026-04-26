using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketHomingData : IComponentData
    {
        public float TurnRateRadPerSec;
        public Entity TargetEntity;
    }
}
