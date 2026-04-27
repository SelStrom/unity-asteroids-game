using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsLaserSystem))]
    public partial struct EcsMissileLauncherSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MissileShootEvent>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var events = SystemAPI.GetSingletonBuffer<MissileShootEvent>();

            foreach (var (launcher, entity) in
                     SystemAPI.Query<RefRW<MissileLauncherData>>().WithEntityAccess())
            {
                if (launcher.ValueRO.CurrentShoots < launcher.ValueRO.MaxShoots)
                {
                    launcher.ValueRW.ReloadRemaining -= deltaTime;
                    if (launcher.ValueRO.ReloadRemaining <= 0f)
                    {
                        launcher.ValueRW.ReloadRemaining = launcher.ValueRO.ReloadDurationSec;
                        launcher.ValueRW.CurrentShoots += 1;
                    }
                }

                if (launcher.ValueRO.Shooting && launcher.ValueRO.CurrentShoots > 0)
                {
                    launcher.ValueRW.CurrentShoots -= 1;
                    events.Add(new MissileShootEvent
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
