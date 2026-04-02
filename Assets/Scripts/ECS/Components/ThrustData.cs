using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct ThrustData : IComponentData
    {
        public const float MinSpeed = 0.0f;

        public float UnitsPerSecond;
        public float MaxSpeed;
        public bool IsActive;
    }
}
