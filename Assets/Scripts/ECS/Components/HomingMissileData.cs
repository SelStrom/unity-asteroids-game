using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct HomingMissileData : IComponentData
    {
        public float TurnRateRadPerSec;
        public float SeekRange;
    }
}
