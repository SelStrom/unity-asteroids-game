using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [BurstCompile]
    [UpdateAfter(typeof(EcsRotateSystem))]
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsThrustSystem : ISystem
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

            foreach (var (thrust, move, rotate) in
                     SystemAPI.Query<RefRO<ThrustData>, RefRW<MoveData>, RefRO<RotateData>>())
            {
                if (thrust.ValueRO.IsActive)
                {
                    var acceleration = thrust.ValueRO.UnitsPerSecond * deltaTime;
                    var velocity = move.ValueRO.Direction * move.ValueRO.Speed +
                                   rotate.ValueRO.Rotation * acceleration;
                    move.ValueRW.Direction = math.normalizesafe(velocity);
                    move.ValueRW.Speed = math.min(math.length(velocity), thrust.ValueRO.MaxSpeed);
                }
                else
                {
                    move.ValueRW.Speed = math.max(
                        move.ValueRO.Speed - thrust.ValueRO.UnitsPerSecond / 2f * deltaTime,
                        ThrustData.MinSpeed
                    );
                }
            }
        }
    }
}
