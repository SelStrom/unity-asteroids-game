using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Самонаведение ракет: каждый кадр выбирает ближайшего врага (астероид/UFO) и доворачивает
    /// направление движения с ограничением скорости поворота, формируя траекторию-дугу.
    /// Перемещение выполняет <see cref="EcsMoveSystem"/>. Если целей нет — ракета летит прямо.
    /// </summary>
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsHomingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (move, rotate, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RotateData>, RefRO<HomingData>>()
                         .WithAll<RocketTag>())
            {
                var rocketPos = move.ValueRO.Position;

                // Поиск ближайшего врага (инлайн: SystemAPI.Query требует instance-контекст OnUpdate).
                var bestDistSq = float.MaxValue;
                var nearest = float2.zero;
                var found = false;
                foreach (var enemyMove in
                         SystemAPI.Query<RefRO<MoveData>>()
                             .WithAny<AsteroidTag, UfoTag, UfoBigTag>())
                {
                    var distSq = math.distancesq(rocketPos, enemyMove.ValueRO.Position);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        nearest = enemyMove.ValueRO.Position;
                        found = true;
                    }
                }

                if (!found)
                {
                    continue;
                }

                var desired = math.normalizesafe(nearest - rocketPos);
                if (math.lengthsq(desired) < 1e-8f)
                {
                    continue;
                }

                var current = math.normalizesafe(move.ValueRO.Direction, desired);
                var newDir = StepTowards(current, desired,
                    math.radians(homing.ValueRO.TurnRateDegPerSec) * deltaTime);

                move.ValueRW.Direction = newDir;
                rotate.ValueRW.Rotation = newDir;
            }
        }

        /// <summary>
        /// Поворачивает <paramref name="current"/> к <paramref name="desired"/> не более чем
        /// на <paramref name="maxStepRad"/> радиан и возвращает единичный вектор.
        /// </summary>
        private static float2 StepTowards(float2 current, float2 desired, float maxStepRad)
        {
            var cross = current.x * desired.y - current.y * desired.x;
            var dot = math.dot(current, desired);
            var angle = math.atan2(cross, dot);

            var step = math.clamp(angle, -maxStepRad, maxStepRad);

            var cos = math.cos(step);
            var sin = math.sin(step);
            return math.normalizesafe(new float2(
                current.x * cos - current.y * sin,
                current.x * sin + current.y * cos), desired);
        }
    }
}
