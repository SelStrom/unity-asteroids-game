using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsRocketHomingSystem : ISystem
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

            foreach (var (move, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RocketHomingData>>().WithAll<RocketTag>())
            {
                var position = move.ValueRO.Position;

                if (!IsTargetValid(ref em, homing.ValueRO.TargetEntity))
                {
                    homing.ValueRW.TargetEntity = FindNearestTarget(ref em, targets, position);
                }

                var target = homing.ValueRO.TargetEntity;
                if (target == Entity.Null)
                {
                    continue;
                }

                var targetPosition = em.GetComponentData<MoveData>(target).Position;
                move.ValueRW.Direction = RotateTowards(
                    move.ValueRO.Direction,
                    targetPosition - position,
                    math.radians(homing.ValueRO.TurnRateDegPerSec) * deltaTime);
            }

            targets.Dispose();
        }

        private static bool IsTargetValid(ref EntityManager em, Entity target)
        {
            return target != Entity.Null
                   && em.Exists(target)
                   && em.HasComponent<MoveData>(target)
                   && !em.HasComponent<DeadTag>(target);
        }

        private static Entity FindNearestTarget(
            ref EntityManager em, NativeArray<Entity> targets, float2 position)
        {
            var nearest = Entity.Null;
            var nearestDistanceSq = float.MaxValue;
            for (var i = 0; i < targets.Length; i++)
            {
                var targetPosition = em.GetComponentData<MoveData>(targets[i]).Position;
                var distanceSq = math.distancesq(position, targetPosition);
                if (distanceSq < nearestDistanceSq)
                {
                    nearestDistanceSq = distanceSq;
                    nearest = targets[i];
                }
            }

            return nearest;
        }

        private static float2 RotateTowards(float2 current, float2 desired, float maxRadians)
        {
            var currentAngle = math.atan2(current.y, current.x);
            var desiredAngle = math.atan2(desired.y, desired.x);
            // Разница углов с нормализацией в [-PI, PI]
            var delta = math.atan2(
                math.sin(desiredAngle - currentAngle),
                math.cos(desiredAngle - currentAngle));
            var newAngle = currentAngle + math.clamp(delta, -maxRadians, maxRadians);
            return new float2(math.cos(newAngle), math.sin(newAngle));
        }
    }
}
