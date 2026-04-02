using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct MoveData : IComponentData
    {
        public float2 Position;
        public float Speed;
        public float2 Direction;
    }
}
