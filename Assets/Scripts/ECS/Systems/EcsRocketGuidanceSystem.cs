using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsMoveSystem))]
    public partial class EcsRocketGuidanceSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (move, target, entity) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RocketTargetData>>()
                         .WithAll<RocketTag>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                // 1. Проверить валидность текущей цели
                if (target.ValueRO.Target != Entity.Null)
                {
                    if (!EntityManager.Exists(target.ValueRO.Target) ||
                        EntityManager.HasComponent<DeadTag>(target.ValueRO.Target))
                    {
                        target.ValueRW.Target = Entity.Null;
                    }
                }

                // 2. Если цели нет -- найти ближайшего врага
                if (target.ValueRO.Target == Entity.Null)
                {
                    target.ValueRW.Target = FindClosestEnemy(move.ValueRO.Position);
                }

                // 3. Если цель найдена -- повернуть Direction к цели
                if (target.ValueRO.Target != Entity.Null)
                {
                    var targetPos = EntityManager.GetComponentData<MoveData>(target.ValueRO.Target).Position;
                    var toTarget = math.normalizesafe(targetPos - move.ValueRO.Position);

                    // Защита от случая, когда ракета и цель на одной точке
                    if (!math.all(toTarget == float2.zero))
                    {
                        move.ValueRW.Direction = RotateTowards(
                            move.ValueRO.Direction,
                            toTarget,
                            target.ValueRO.TurnRateDegPerSec,
                            deltaTime);
                    }
                }
                // Если цели нет -- Direction не меняется (ракета летит прямо)
            }
        }

        private Entity FindClosestEnemy(float2 rocketPosition)
        {
            var closestEntity = Entity.Null;
            var closestDistSq = float.MaxValue;

            // Поиск среди астероидов
            foreach (var (move, entity) in
                     SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<AsteroidTag>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                var distSq = math.distancesq(rocketPosition, move.ValueRO.Position);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestEntity = entity;
                }
            }

            // Поиск среди больших UFO
            foreach (var (move, entity) in
                     SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<UfoBigTag>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                var distSq = math.distancesq(rocketPosition, move.ValueRO.Position);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestEntity = entity;
                }
            }

            // Поиск среди малых UFO
            foreach (var (move, entity) in
                     SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<UfoTag>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                var distSq = math.distancesq(rocketPosition, move.ValueRO.Position);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        internal static float2 RotateTowards(float2 current, float2 target, float turnRateDeg, float deltaTime)
        {
            var maxAngle = math.radians(turnRateDeg * deltaTime);

            var cross = current.x * target.y - current.y * target.x;
            var dot = math.dot(current, target);
            var angle = math.acos(math.clamp(dot, -1f, 1f));

            // Если угол до цели меньше максимального поворота -- вернуть цель напрямую
            if (angle <= maxAngle)
            {
                return target;
            }

            // Повернуть на maxAngle в направлении цели (знак cross определяет сторону)
            var rotAngle = math.sign(cross) * maxAngle;
            var cos = math.cos(rotAngle);
            var sin = math.sin(rotAngle);
            return math.normalizesafe(new float2(
                current.x * cos - current.y * sin,
                current.x * sin + current.y * cos
            ));
        }
    }
}
