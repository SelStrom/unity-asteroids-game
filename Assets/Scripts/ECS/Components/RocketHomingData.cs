using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketHomingData : IComponentData
    {
        public Entity TargetEntity;
        public float TurnRateDegPerSec;
    }
}
