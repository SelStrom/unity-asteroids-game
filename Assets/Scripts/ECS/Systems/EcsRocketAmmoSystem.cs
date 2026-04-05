using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsLaserSystem))]
    public partial struct EcsRocketAmmoSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RocketShootEvent>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var rocketEvents = SystemAPI.GetSingletonBuffer<RocketShootEvent>();

            foreach (var (ammo, entity) in SystemAPI.Query<RefRW<RocketAmmoData>>().WithEntityAccess())
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

                if (ammo.ValueRO.Shooting && ammo.ValueRO.CurrentAmmo > 0)
                {
                    ammo.ValueRW.CurrentAmmo--;
                    rocketEvents.Add(new RocketShootEvent
                    {
                        ShooterEntity = entity,
                        Position = ammo.ValueRO.ShootPosition,
                        Direction = ammo.ValueRO.Direction
                    });
                }

                ammo.ValueRW.Shooting = false;
            }
        }
    }
}
