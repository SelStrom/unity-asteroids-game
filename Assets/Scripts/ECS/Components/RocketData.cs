using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketData : IComponentData
    {
        public Entity TargetEntity;
        public float TurnSpeedDegPerSec;
    }
}
