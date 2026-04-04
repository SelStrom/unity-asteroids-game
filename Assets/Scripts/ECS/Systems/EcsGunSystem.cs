using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    [UpdateBefore(typeof(EcsLaserSystem))]
    public partial struct EcsGunSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GunShootEvent>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var gunEvents = SystemAPI.GetSingletonBuffer<GunShootEvent>();

            foreach (var (gun, entity) in SystemAPI.Query<RefRW<GunData>>().WithEntityAccess())
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
                    gunEvents.Add(new GunShootEvent
                    {
                        ShooterEntity = entity,
                        Position = gun.ValueRO.ShootPosition,
                        Direction = gun.ValueRO.Direction,
                        IsPlayer = state.EntityManager.HasComponent<ShipTag>(entity)
                    });
                }

                gun.ValueRW.Shooting = false;
            }
        }
    }
}
