using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsLaserSystem))]
    public partial struct EcsRocketAmmoSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var ammo in SystemAPI.Query<RefRW<RocketAmmoData>>())
            {
                if (ammo.ValueRO.CurrentAmmo < ammo.ValueRO.MaxAmmo)
                {
                    ammo.ValueRW.ReloadRemaining -= deltaTime;
                    if (ammo.ValueRO.ReloadRemaining <= 0)
                    {
                        ammo.ValueRW.ReloadRemaining = ammo.ValueRO.ReloadDurationSec;
                        ammo.ValueRW.CurrentAmmo += 1;
                    }
                }
            }
        }
    }
}
