using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsLaserSystem))]
    public partial struct EcsRocketLaunchSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RocketLaunchEvent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var launchEvents = SystemAPI.GetSingletonBuffer<RocketLaunchEvent>();

            foreach (var (ammo, entity) in
                     SystemAPI.Query<RefRW<RocketAmmoData>>().WithEntityAccess())
            {
                if (ammo.ValueRO.CurrentRockets < ammo.ValueRO.MaxRockets)
                {
                    ammo.ValueRW.RespawnRemaining -= deltaTime;
                    if (ammo.ValueRO.RespawnRemaining <= 0)
                    {
                        ammo.ValueRW.RespawnRemaining = ammo.ValueRO.RespawnDurationSec;
                        ammo.ValueRW.CurrentRockets += 1;
                    }
                }

                if (ammo.ValueRO.Launching && ammo.ValueRO.CurrentRockets > 0)
                {
                    ammo.ValueRW.CurrentRockets -= 1;
                    launchEvents.Add(new RocketLaunchEvent
                    {
                        ShooterEntity = entity,
                        Position = ammo.ValueRO.LaunchPosition,
                        Direction = ammo.ValueRO.Direction
                    });
                }

                ammo.ValueRW.Launching = false;
            }
        }
    }
}
