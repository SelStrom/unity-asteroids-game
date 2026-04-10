using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct MissileData : IComponentData
    {
        public int MaxShoots;
        public float ReloadDurationSec;
        public int CurrentShoots;
        public float ReloadRemaining;
        public bool Shooting;
        public float2 Direction;
        public float2 ShootPosition;
    }
}
