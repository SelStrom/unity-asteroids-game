using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct RotateData : IComponentData
    {
        public const float DegreePerSecond = 90f;

        public float TargetDirection;
        public float2 Rotation;
    }
}
