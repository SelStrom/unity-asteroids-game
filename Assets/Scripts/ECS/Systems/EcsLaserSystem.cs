using Unity.Burst;
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    // [UpdateAfter(typeof(EcsGunSystem))] -- будет добавлено при интеграции порядка систем
    // [UpdateBefore(typeof(EcsShootToSystem))] -- будет добавлено после создания EcsShootToSystem
    public partial struct EcsLaserSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var laser in SystemAPI.Query<RefRW<LaserData>>())
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
                    // OnShooting callback -- Phase 5 Bridge Layer
                }

                laser.ValueRW.Shooting = false;
            }
        }
    }
}
