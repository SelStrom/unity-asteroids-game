using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsLaserSystem))]
    public partial struct EcsRocketLauncherSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RocketLaunchEvent>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var rocketEvents = SystemAPI.GetSingletonBuffer<RocketLaunchEvent>();

            foreach (var (launcher, entity) in SystemAPI.Query<RefRW<RocketLauncherData>>().WithEntityAccess())
            {
                if (launcher.ValueRO.CurrentRockets < launcher.ValueRO.MaxRockets)
                {
                    launcher.ValueRW.RespawnRemaining -= deltaTime;
                    if (launcher.ValueRO.RespawnRemaining <= 0)
                    {
                        launcher.ValueRW.RespawnRemaining = launcher.ValueRO.RespawnDurationSec;
                        launcher.ValueRW.CurrentRockets += 1;
                    }
                }

                if (launcher.ValueRO.Shooting && launcher.ValueRO.CurrentRockets > 0)
                {
                    launcher.ValueRW.CurrentRockets -= 1;
                    rocketEvents.Add(new RocketLaunchEvent
                    {
                        ShooterEntity = entity,
                        Position = launcher.ValueRO.ShootPosition,
                        Direction = launcher.ValueRO.Direction
                    });
                }

                launcher.ValueRW.Shooting = false;
            }
        }
    }
}
