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

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;

            // Живые цели собираются заранее: вложенные запросы внутри цикла недопустимы
            var targetEntities = new NativeList<Entity>(Allocator.Temp);
            var targetPositions = new NativeList<float2>(Allocator.Temp);
            foreach (var (move, entity) in
                     SystemAPI.Query<RefRO<MoveData>>()
                         .WithAny<AsteroidTag, UfoTag, UfoBigTag>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                targetEntities.Add(entity);
                targetPositions.Add(move.ValueRO.Position);
            }

            foreach (var (move, rotate, rocket) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RotateData>, RefRW<RocketData>>()
                         .WithAll<RocketTag>())
            {
                var position = move.ValueRO.Position;

                var target = rocket.ValueRO.TargetEntity;
                if (!IsAliveTarget(ref em, target))
                {
                    target = FindNearestTarget(position, targetEntities, targetPositions);
                    rocket.ValueRW.TargetEntity = target;
                }

                if (target == Entity.Null)
                {
                    continue;
                }

                var targetPosition = em.GetComponentData<MoveData>(target).Position;
                var desired = math.normalizesafe(targetPosition - position);
                if (math.lengthsq(desired) == 0f)
                {
                    continue;
                }

                var currentAngle = math.atan2(move.ValueRO.Direction.y, move.ValueRO.Direction.x);
                var desiredAngle = math.atan2(desired.y, desired.x);

                // Кратчайший знаковый угол доворота, ограниченный угловой скоростью —
                // так траектория получается дугой
                var delta = math.atan2(
                    math.sin(desiredAngle - currentAngle),
                    math.cos(desiredAngle - currentAngle));
                var maxTurn = math.radians(rocket.ValueRO.TurnSpeedDegPerSec) * deltaTime;
                var turn = math.clamp(delta, -maxTurn, maxTurn);

                var newAngle = currentAngle + turn;
                var newDirection = new float2(math.cos(newAngle), math.sin(newAngle));
                move.ValueRW.Direction = newDirection;
                rotate.ValueRW.Rotation = newDirection;
            }

            targetEntities.Dispose();
            targetPositions.Dispose();
        }

        private bool IsAliveTarget(ref EntityManager em, Entity target)
        {
            return target != Entity.Null
                   && em.Exists(target)
                   && !em.HasComponent<DeadTag>(target)
                   && em.HasComponent<MoveData>(target);
        }

        private Entity FindNearestTarget(
            float2 position, NativeList<Entity> entities, NativeList<float2> positions)
        {
            var nearest = Entity.Null;
            var nearestDistanceSq = float.MaxValue;
            for (var i = 0; i < entities.Length; i++)
            {
                var distanceSq = math.distancesq(position, positions[i]);
                if (distanceSq < nearestDistanceSq)
                {
                    nearestDistanceSq = distanceSq;
                    nearest = entities[i];
                }
            }

            return nearest;
        }
    }
}
