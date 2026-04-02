using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.ECS
{
    [BurstCompile]
    [UpdateBefore(typeof(EcsThrustSystem))]
    public partial struct EcsRotateSystem : ISystem
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

            foreach (var rotate in SystemAPI.Query<RefRW<RotateData>>())
            {
                if (rotate.ValueRO.TargetDirection == 0)
                {
                    continue;
                }

                var angle = math.radians(RotateData.DegreePerSecond * deltaTime * rotate.ValueRO.TargetDirection);
                var cos = math.cos(angle);
                var sin = math.sin(angle);
                var current = rotate.ValueRO.Rotation;
                rotate.ValueRW.Rotation = new float2(
                    current.x * cos - current.y * sin,
                    current.x * sin + current.y * cos
                );
            }
        }
    }
}
