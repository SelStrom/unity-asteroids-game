using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketTargetData : IComponentData
    {
        public Entity Target;
        public float TurnRateDegPerSec;
    }
}
