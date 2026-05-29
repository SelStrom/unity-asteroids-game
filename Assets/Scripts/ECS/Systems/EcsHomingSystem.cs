using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Самонаведение ракет. Для каждой ракеты ищет ближайшего врага
    /// (астероид / UFO / большой UFO) и поворачивает её направление к цели
    /// не более чем на TurnRateDegPerSec за кадр — отсюда полёт по дуге.
    /// Обновляется до EcsMoveSystem, чтобы новое направление сразу применилось.
    /// </summary>
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsHomingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RocketTag>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var enemies = new NativeList<float2>(Allocator.Temp);
            foreach (var move in SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<AsteroidTag>().WithNone<DeadTag>())
            {
                enemies.Add(move.ValueRO.Position);
            }

            foreach (var move in SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<UfoTag>().WithNone<DeadTag>())
            {
                enemies.Add(move.ValueRO.Position);
            }

            foreach (var move in SystemAPI.Query<RefRO<MoveData>>()
                         .WithAll<UfoBigTag>().WithNone<DeadTag>())
            {
                enemies.Add(move.ValueRO.Position);
            }

            if (enemies.Length == 0)
            {
                enemies.Dispose();
                return;
            }

            foreach (var (move, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRO<HomingData>>()
                         .WithAll<RocketTag>().WithNone<DeadTag>())
            {
                var position = move.ValueRO.Position;

                var nearest = enemies[0];
                var bestDistSq = math.distancesq(position, nearest);
                for (var i = 1; i < enemies.Length; i++)
                {
                    var distSq = math.distancesq(position, enemies[i]);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        nearest = enemies[i];
                    }
                }

                var desired = math.normalizesafe(nearest - position);
                if (math.lengthsq(desired) < 1e-6f)
                {
                    continue;
                }

                var current = math.normalizesafe(move.ValueRO.Direction, desired);
                var maxTurnRad = math.radians(homing.ValueRO.TurnRateDegPerSec * deltaTime);
                move.ValueRW.Direction = RotateToward(current, desired, maxTurnRad);
            }

            enemies.Dispose();
        }

        /// <summary>
        /// Поворачивает единичный вектор current к desired не более чем на maxRad.
        /// </summary>
        private static float2 RotateToward(float2 current, float2 desired, float maxRad)
        {
            var dot = math.clamp(math.dot(current, desired), -1f, 1f);
            var angleBetween = math.acos(dot);

            if (angleBetween <= maxRad)
            {
                return desired;
            }

            // Знак поворота по z-компоненте векторного произведения.
            var cross = current.x * desired.y - current.y * desired.x;
            var signedAngle = cross >= 0f ? maxRad : -maxRad;

            var cos = math.cos(signedAngle);
            var sin = math.sin(signedAngle);
            return new float2(
                current.x * cos - current.y * sin,
                current.x * sin + current.y * cos);
        }
    }
}
