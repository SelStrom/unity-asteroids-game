using Unity.Burst;
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    // [UpdateAfter(typeof(EcsLifeTimeSystem))] -- будет добавлено после создания EcsLifeTimeSystem
    // [UpdateBefore(typeof(EcsLaserSystem))] -- будет добавлено после создания EcsLaserSystem
    public partial struct EcsGunSystem : ISystem
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

            foreach (var gun in SystemAPI.Query<RefRW<GunData>>())
            {
                if (gun.ValueRO.CurrentShoots < gun.ValueRO.MaxShoots)
                {
                    gun.ValueRW.ReloadRemaining -= deltaTime;
                    if (gun.ValueRO.ReloadRemaining <= 0)
                    {
                        gun.ValueRW.ReloadRemaining = gun.ValueRO.ReloadDurationSec;
                        gun.ValueRW.CurrentShoots = gun.ValueRO.MaxShoots;
                    }
                }

                if (gun.ValueRO.Shooting && gun.ValueRO.CurrentShoots > 0)
                {
                    gun.ValueRW.CurrentShoots--;
                    // OnShooting callback -- Phase 5 Bridge Layer
                }

                gun.ValueRW.Shooting = false;
            }
        }
    }
}
