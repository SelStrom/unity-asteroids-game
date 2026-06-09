using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsHomingSystem : ISystem
    {
        private EntityQuery _targetsQuery;

        public void OnCreate(ref SystemState state)
        {
            _targetsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MoveData>()
                .WithAny<AsteroidTag, UfoTag, UfoBigTag>()
                .WithNone<DeadTag>()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;

            var targets = _targetsQuery.ToEntityArray(Allocator.Temp);
            var targetMoves = _targetsQuery.ToComponentDataArray<MoveData>(Allocator.Temp);

            foreach (var (move, rotate, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RotateData>, RefRW<HomingData>>()
                         .WithAll<RocketTag>())
            {
                var target = homing.ValueRO.TargetEntity;
                var targetIsValid = target != Entity.Null
                                    && em.Exists(target)
                                    && !em.HasComponent<DeadTag>(target)
                                    && em.HasComponent<MoveData>(target);
                if (!targetIsValid)
                {
                    target = FindNearest(targets, targetMoves, move.ValueRO.Position);
                    homing.ValueRW.TargetEntity = target;
                }

                if (target == Entity.Null)
                {
                    continue;
                }

                var targetPosition = em.GetComponentData<MoveData>(target).Position;
                var toTarget = targetPosition - move.ValueRO.Position;
                if (math.lengthsq(toTarget) < 1e-6f)
                {
                    continue;
                }

                var current = move.ValueRO.Direction;
                var desiredAngle = math.atan2(toTarget.y, toTarget.x);
                var currentAngle = math.atan2(current.y, current.x);
                var delta = desiredAngle - currentAngle;
                delta = math.atan2(math.sin(delta), math.cos(delta));

                var maxTurn = math.radians(homing.ValueRO.TurnSpeedDegPerSec) * deltaTime;
                var newAngle = currentAngle + math.clamp(delta, -maxTurn, maxTurn);
                var direction = new float2(math.cos(newAngle), math.sin(newAngle));

                move.ValueRW.Direction = direction;
                rotate.ValueRW.Rotation = direction;
            }

            targets.Dispose();
            targetMoves.Dispose();
        }

        private static Entity FindNearest(
            in NativeArray<Entity> targets, in NativeArray<MoveData> targetMoves, float2 position)
        {
            var nearest = Entity.Null;
            var nearestDistanceSq = float.MaxValue;
            for (var i = 0; i < targets.Length; i++)
            {
                var distanceSq = math.distancesq(targetMoves[i].Position, position);
                if (distanceSq < nearestDistanceSq)
                {
                    nearestDistanceSq = distanceSq;
                    nearest = targets[i];
                }
            }

            return nearest;
        }
    }
}
