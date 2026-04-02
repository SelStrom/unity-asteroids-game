using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct GameAreaData : IComponentData
    {
        public float2 Size;
    }
}
