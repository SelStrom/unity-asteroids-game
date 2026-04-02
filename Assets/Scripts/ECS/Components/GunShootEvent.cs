using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct GunShootEvent : IBufferElementData
    {
        public Entity ShooterEntity;
        public float2 Position;
        public float2 Direction;
        public bool IsPlayer;
    }
}
