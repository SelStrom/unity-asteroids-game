using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateBefore(typeof(EcsHomingSystem))]
    public partial struct EcsRocketSystem : ISystem
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

            foreach (var (rocket, entity) in SystemAPI.Query<RefRW<RocketData>>().WithEntityAccess())
            {
                if (rocket.ValueRO.CurrentShoots < rocket.ValueRO.MaxShoots)
                {
                    rocket.ValueRW.ReloadRemaining -= deltaTime;
                    if (rocket.ValueRO.ReloadRemaining <= 0)
                    {
                        rocket.ValueRW.ReloadRemaining = rocket.ValueRO.ReloadDurationSec;
                        rocket.ValueRW.CurrentShoots += 1;
                    }
                }

                if (rocket.ValueRO.Shooting && rocket.ValueRO.CurrentShoots > 0)
                {
                    rocket.ValueRW.CurrentShoots--;
                    rocketEvents.Add(new RocketShootEvent
                    {
                        ShooterEntity = entity,
                        Position = rocket.ValueRO.ShootPosition,
                        Direction = rocket.ValueRO.Direction
                    });
                }

                rocket.ValueRW.Shooting = false;
            }
        }
    }
}
