using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Управляет боезапасом ракет игрока: пополняет по одной каждые RespawnDurationSec
    /// (до MaxRockets) и эмитит RocketLaunchEvent при запуске. Зеркало <see cref="EcsGunSystem"/>.
    /// </summary>
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    public partial struct EcsRocketLauncherSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RocketLaunchEvent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var launchEvents = SystemAPI.GetSingletonBuffer<RocketLaunchEvent>();

            foreach (var launcher in SystemAPI.Query<RefRW<RocketLauncherData>>())
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
                    launchEvents.Add(new RocketLaunchEvent
                    {
                        Position = launcher.ValueRO.LaunchPosition,
                        Direction = launcher.ValueRO.LaunchDirection
                    });
                }

                launcher.ValueRW.Launching = false;
            }
        }
    }
}
