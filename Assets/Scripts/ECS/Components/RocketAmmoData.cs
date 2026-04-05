using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketAmmoData : IComponentData
    {
        public int MaxAmmo;
        public float ReloadDurationSec;
        public int CurrentAmmo;
        public float ReloadRemaining;
    }
}
