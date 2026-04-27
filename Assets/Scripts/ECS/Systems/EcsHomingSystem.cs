using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsRocketSystem))]
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsHomingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameAreaData>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (move, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRO<HomingData>>()
                         .WithAll<RocketTag>()
                         .WithNone<DeadTag>())
            {
                var rocketPos = move.ValueRO.Position;
                var nearestPos = float2.zero;
                var nearestDistSq = float.MaxValue;
                var hasTarget = false;

                foreach (var asteroidMove in SystemAPI.Query<RefRO<MoveData>>()
                             .WithAll<AsteroidTag>()
                             .WithNone<DeadTag>())
                {
                    var distSq = math.distancesq(rocketPos, asteroidMove.ValueRO.Position);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestPos = asteroidMove.ValueRO.Position;
                        hasTarget = true;
                    }
                }

                foreach (var ufoMove in SystemAPI.Query<RefRO<MoveData>>()
                             .WithAll<UfoTag>()
                             .WithNone<DeadTag>())
                {
                    var distSq = math.distancesq(rocketPos, ufoMove.ValueRO.Position);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestPos = ufoMove.ValueRO.Position;
                        hasTarget = true;
                    }
                }

                foreach (var ufoBigMove in SystemAPI.Query<RefRO<MoveData>>()
                             .WithAll<UfoBigTag>()
                             .WithNone<DeadTag>())
                {
                    var distSq = math.distancesq(rocketPos, ufoBigMove.ValueRO.Position);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestPos = ufoBigMove.ValueRO.Position;
                        hasTarget = true;
                    }
                }

                if (!hasTarget)
                {
                    continue;
                }

                var toTarget = math.normalizesafe(nearestPos - rocketPos);
                var currentDir = move.ValueRO.Direction;

                var cross = currentDir.x * toTarget.y - currentDir.y * toTarget.x;
                var maxAngle = math.radians(homing.ValueRO.TurnSpeed) * deltaTime;

                var angle = math.clamp(cross, -1f, 1f);
                var turnAngle = math.clamp(math.asin(angle), -maxAngle, maxAngle);

                var cos = math.cos(turnAngle);
                var sin = math.sin(turnAngle);
                move.ValueRW.Direction = math.normalizesafe(new float2(
                    currentDir.x * cos - currentDir.y * sin,
                    currentDir.x * sin + currentDir.y * cos
                ));
            }
        }
    }
}
