using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public partial struct EcsLaserSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LaserShootEvent>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var laserEvents = SystemAPI.GetSingletonBuffer<LaserShootEvent>();

            foreach (var (laser, entity) in SystemAPI.Query<RefRW<LaserData>>().WithEntityAccess())
            {
                if (laser.ValueRO.CurrentShoots < laser.ValueRO.MaxShoots)
                {
                    laser.ValueRW.ReloadRemaining -= deltaTime;
                    if (laser.ValueRO.ReloadRemaining <= 0)
                    {
                        laser.ValueRW.ReloadRemaining = laser.ValueRO.UpdateDurationSec;
                        laser.ValueRW.CurrentShoots += 1;
                    }
                }

                if (laser.ValueRO.Shooting && laser.ValueRO.CurrentShoots > 0)
                {
                    laser.ValueRW.CurrentShoots -= 1;
                    laserEvents.Add(new LaserShootEvent
                    {
                        ShooterEntity = entity,
                        Position = laser.ValueRO.ShootPosition,
                        Direction = laser.ValueRO.Direction
                    });
                }

                laser.ValueRW.Shooting = false;
            }
        }
    }
}
