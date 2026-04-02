using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct MoveToData : IComponentData
    {
        public float Every;
        public float ReadyRemaining;
    }
}
