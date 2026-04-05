using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketAmmoData : IComponentData
    {
        public int MaxAmmo;
        public float ReloadDurationSec;
        public int CurrentAmmo;
        public float ReloadRemaining;
        public bool Shooting;
        public float2 Direction;
        public float2 ShootPosition;
    }
}
