using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketData : IComponentData
    {
        public float TurnSpeedRadPerSec;
    }
}
