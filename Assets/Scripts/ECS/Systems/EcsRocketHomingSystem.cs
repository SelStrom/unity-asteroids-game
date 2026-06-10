using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsRocketHomingSystem : ISystem
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

            var targetPositions = new NativeList<float2>(Allocator.Temp);
            foreach (var move in SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<AsteroidTag>().WithNone<DeadTag>())
            {
                targetPositions.Add(move.ValueRO.Position);
            }

            foreach (var move in SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<UfoTag>().WithNone<DeadTag>())
            {
                targetPositions.Add(move.ValueRO.Position);
            }

            foreach (var move in SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<UfoBigTag>().WithNone<DeadTag>())
            {
                targetPositions.Add(move.ValueRO.Position);
            }

            if (targetPositions.Length == 0)
            {
                targetPositions.Dispose();
                return;
            }

            foreach (var (move, rotate, rocket) in SystemAPI
                         .Query<RefRW<MoveData>, RefRW<RotateData>, RefRO<RocketData>>()
                         .WithAll<RocketTag>())
            {
                var position = move.ValueRO.Position;
                var nearest = targetPositions[0];
                var nearestDistanceSq = math.distancesq(position, nearest);
                for (var i = 1; i < targetPositions.Length; i++)
                {
                    var distanceSq = math.distancesq(position, targetPositions[i]);
                    if (distanceSq < nearestDistanceSq)
                    {
                        nearestDistanceSq = distanceSq;
                        nearest = targetPositions[i];
                    }
                }

                var current = math.normalizesafe(move.ValueRO.Direction, new float2(1f, 0f));
                var toTarget = math.normalizesafe(nearest - position, current);
                var signedAngle = math.atan2(
                    current.x * toTarget.y - current.y * toTarget.x,
                    math.dot(current, toTarget));
                var maxTurn = rocket.ValueRO.TurnSpeedRadPerSec * deltaTime;
                var turn = math.clamp(signedAngle, -maxTurn, maxTurn);

                math.sincos(turn, out var sin, out var cos);
                var newDirection = new float2(
                    current.x * cos - current.y * sin,
                    current.x * sin + current.y * cos);

                move.ValueRW.Direction = newDirection;
                rotate.ValueRW.Rotation = newDirection;
            }

            targetPositions.Dispose();
        }
    }
}
