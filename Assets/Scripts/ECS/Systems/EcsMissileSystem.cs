using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsLaserSystem))]
    public partial struct EcsMissileSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MissileSpawnEvent>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var missileEvents = SystemAPI.GetSingletonBuffer<MissileSpawnEvent>();

            foreach (var (missile, entity) in SystemAPI.Query<RefRW<MissileData>>().WithEntityAccess())
            {
                if (missile.ValueRO.CurrentShoots < missile.ValueRO.MaxShoots)
                {
                    missile.ValueRW.RespawnRemaining -= deltaTime;
                    if (missile.ValueRO.RespawnRemaining <= 0)
                    {
                        missile.ValueRW.RespawnRemaining = missile.ValueRO.RespawnDurationSec;
                        missile.ValueRW.CurrentShoots += 1;
                    }
                }

                if (missile.ValueRO.Shooting && missile.ValueRO.CurrentShoots > 0)
                {
                    missile.ValueRW.CurrentShoots -= 1;
                    missileEvents.Add(new MissileSpawnEvent
                    {
                        ShooterEntity = entity,
                        Position = missile.ValueRO.ShootPosition,
                        Direction = missile.ValueRO.Direction
                    });
                }

                missile.ValueRW.Shooting = false;
            }
        }
    }
}
