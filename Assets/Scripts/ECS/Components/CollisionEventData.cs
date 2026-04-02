using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct CollisionEventData : IBufferElementData
    {
        public Entity EntityA;
        public Entity EntityB;
    }
}
