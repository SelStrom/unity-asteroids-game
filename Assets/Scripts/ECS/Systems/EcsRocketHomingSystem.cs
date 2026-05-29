using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Самонаведение ракеты. Каждый кадр выбирает ближайшего живого врага (если текущая цель
    /// невалидна/уничтожена) и доворачивает направление движения к цели не более чем на
    /// <c>TurnRateDegPerSec * dt</c>. Ограниченная скорость поворота даёт полёт по дуге.
    /// Также пишет визуальный поворот в <see cref="RotateData.Rotation"/>.
    /// </summary>
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    public partial struct EcsRocketHomingSystem : ISystem
    {
        private struct EnemyInfo
        {
            public Entity Entity;
            public float2 Position;
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RocketData>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;

            var enemies = new NativeList<EnemyInfo>(Allocator.Temp);
            foreach (var (move, entity) in
                     SystemAPI.Query<RefRO<MoveData>>()
                         .WithAny<AsteroidTag, UfoTag, UfoBigTag>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                enemies.Add(new EnemyInfo
                {
                    Entity = entity,
                    Position = move.ValueRO.Position
                });
            }

            foreach (var (move, rocket, rotate) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RocketData>, RefRW<RotateData>>())
            {
                var position = move.ValueRO.Position;

                if (!IsValidTarget(ref em, rocket.ValueRO.Target))
                {
                    rocket.ValueRW.Target = FindNearest(enemies, position);
                }

                if (rocket.ValueRO.Target == Entity.Null)
                {
                    // Целей нет — продолжаем лететь прямо, обновляем визуальный поворот.
                    rotate.ValueRW.Rotation =
                        math.normalizesafe(move.ValueRO.Direction, rotate.ValueRO.Rotation);
                    continue;
                }

                var targetPos = em.GetComponentData<MoveData>(rocket.ValueRO.Target).Position;
                var desired = math.normalizesafe(targetPos - position, move.ValueRO.Direction);
                var maxTurn = math.radians(rocket.ValueRO.TurnRateDegPerSec) * deltaTime;
                var newDir = Steer(move.ValueRO.Direction, desired, maxTurn);

                move.ValueRW.Direction = newDir;
                rotate.ValueRW.Rotation = newDir;
            }

            enemies.Dispose();
        }

        private bool IsValidTarget(ref EntityManager em, Entity target)
        {
            return target != Entity.Null
                   && em.Exists(target)
                   && !em.HasComponent<DeadTag>(target)
                   && em.HasComponent<MoveData>(target)
                   && (em.HasComponent<AsteroidTag>(target)
                       || em.HasComponent<UfoTag>(target)
                       || em.HasComponent<UfoBigTag>(target));
        }

        private static Entity FindNearest(NativeList<EnemyInfo> enemies, float2 position)
        {
            var best = Entity.Null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < enemies.Length; i++)
            {
                var d = math.distancesq(enemies[i].Position, position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = enemies[i].Entity;
                }
            }

            return best;
        }

        /// <summary>
        /// Доворачивает единичный вектор <paramref name="current"/> к <paramref name="desired"/>
        /// не более чем на <paramref name="maxTurnRadians"/>. Если угол меньше лимита — возвращает
        /// сразу желаемое направление, иначе — поворот на лимит в сторону цели (знак по cross-product).
        /// </summary>
        public static float2 Steer(float2 current, float2 desired, float maxTurnRadians)
        {
            current = math.normalizesafe(current, new float2(1f, 0f));
            desired = math.normalizesafe(desired, current);

            var dot = math.clamp(math.dot(current, desired), -1f, 1f);
            var angle = math.acos(dot);

            if (angle <= maxTurnRadians || angle <= 1e-5f)
            {
                return desired;
            }

            var cross = current.x * desired.y - current.y * desired.x;
            var sign = cross >= 0f ? 1f : -1f;
            var a = maxTurnRadians * sign;
            var cos = math.cos(a);
            var sin = math.sin(a);
            return new float2(
                current.x * cos - current.y * sin,
                current.x * sin + current.y * cos
            );
        }
    }
}
