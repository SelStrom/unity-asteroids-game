using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.ECS
{
    [BurstCompile]
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    public partial struct EcsLifeTimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var lifeTime in SystemAPI.Query<RefRW<LifeTimeData>>())
            {
                lifeTime.ValueRW.TimeRemaining = math.max(
                    lifeTime.ValueRO.TimeRemaining - deltaTime,
                    0f
                );
            }
        }
    }
}
