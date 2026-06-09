using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct HomingData : IComponentData
    {
        public float TurnSpeedDegPerSec;
        public Entity TargetEntity;
    }
}
