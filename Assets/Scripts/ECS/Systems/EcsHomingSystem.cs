using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsHomingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (move, rotate, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RotateData>, RefRO<HomingData>>()
                         .WithAll<RocketTag>())
            {
                var rocketPos = move.ValueRO.Position;

                var foundEnemy = false;
                var nearestDistSq = float.MaxValue;
                var nearestEnemyPos = float2.zero;

                foreach (var enemyMove in
                         SystemAPI.Query<RefRO<MoveData>>()
                             .WithAny<AsteroidTag, UfoBigTag, UfoTag>())
                {
                    var distSq = math.distancesq(enemyMove.ValueRO.Position, rocketPos);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestEnemyPos = enemyMove.ValueRO.Position;
                        foundEnemy = true;
                    }
                }

                if (!foundEnemy)
                {
                    continue;
                }

                var targetDir = math.normalizesafe(nearestEnemyPos - rocketPos);
                var curDir = move.ValueRO.Direction;

                var maxStep = homing.ValueRO.TurnSpeed * deltaTime;

                // Угол между текущим и целевым направлением (всегда >= 0)
                var dot = math.clamp(math.dot(curDir, targetDir), -1f, 1f);
                var angle = math.acos(dot);

                float2 newDir;
                if (angle <= maxStep)
                {
                    // Поворот меньше максимального шага — выравниваемся точно
                    newDir = targetDir;
                }
                else
                {
                    // Знак поворота: 2D cross product (curDir x targetDir)
                    // cross > 0 — targetDir левее curDir (поворот против часовой)
                    // cross < 0 — targetDir правее curDir (поворот по часовой)
                    var cross = curDir.x * targetDir.y - curDir.y * targetDir.x;
                    var sign = math.sign(cross);
                    if (sign == 0f)
                    {
                        // Направления противоположны — произвольный знак
                        sign = 1f;
                    }

                    var rotAngle = sign * maxStep;
                    var cos = math.cos(rotAngle);
                    var sin = math.sin(rotAngle);
                    newDir = math.normalizesafe(new float2(
                        curDir.x * cos - curDir.y * sin,
                        curDir.x * sin + curDir.y * cos
                    ));
                }

                move.ValueRW.Direction = newDir;
                move.ValueRW.Speed = homing.ValueRO.Speed;
                rotate.ValueRW.Rotation = newDir;
            }
        }
    }
}
