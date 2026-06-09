using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    [UpdateBefore(typeof(EcsGunSystem))]
    public partial struct EcsRocketLauncherSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RocketShootEvent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var rocketEvents = SystemAPI.GetSingletonBuffer<RocketShootEvent>();

            foreach (var (launcher, entity) in
                     SystemAPI.Query<RefRW<RocketLauncherData>>().WithEntityAccess())
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

                if (launcher.ValueRO.Launching && launcher.ValueRO.CurrentRockets > 0)
                {
                    launcher.ValueRW.CurrentRockets -= 1;
                    rocketEvents.Add(new RocketShootEvent
                    {
                        ShooterEntity = entity,
                        Position = launcher.ValueRO.LaunchPosition,
                        Direction = launcher.ValueRO.Direction
                    });
                }

                launcher.ValueRW.Launching = false;
            }
        }
    }
}
