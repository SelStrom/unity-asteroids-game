using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Перезарядка и запуск ракет. Зеркало <see cref="EcsLaserSystem"/>:
    /// пока ракет меньше максимума — тикает респавн (+1 за период);
    /// при запросе на запуск и наличии ракеты — тратит одну и кладёт событие <see cref="RocketLaunchEvent"/>.
    /// </summary>
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
            var launchEvents = SystemAPI.GetSingletonBuffer<RocketLaunchEvent>();

            foreach (var (launcher, entity) in
                     SystemAPI.Query<RefRW<RocketLauncherData>>().WithEntityAccess())
            {
                if (launcher.ValueRO.CurrentRockets < launcher.ValueRO.MaxRockets)
                {
                    launcher.ValueRW.ReloadRemaining -= deltaTime;
                    if (launcher.ValueRO.ReloadRemaining <= 0)
                    {
                        launcher.ValueRW.ReloadRemaining = launcher.ValueRO.ReloadDurationSec;
                        launcher.ValueRW.CurrentRockets += 1;
                    }
                }

                if (launcher.ValueRO.Launching && launcher.ValueRO.CurrentRockets > 0)
                {
                    launcher.ValueRW.CurrentRockets -= 1;
                    launchEvents.Add(new RocketLaunchEvent
                    {
                        ShooterEntity = entity,
                        Position = launcher.ValueRO.LaunchPosition,
                        Direction = launcher.ValueRO.LaunchDirection
                    });
                }

                launcher.ValueRW.Launching = false;
            }
        }
    }
}
