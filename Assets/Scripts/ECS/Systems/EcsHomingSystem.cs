using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsMissileSystem))]
    public partial struct EcsHomingSystem : ISystem
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

            foreach (var (move, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRO<HomingData>>()
                         .WithAll<MissileTag>())
            {
                var missilePos = move.ValueRO.Position;
                var nearestPos = float2.zero;
                var nearestDistSq = float.MaxValue;
                var hasTarget = false;

                foreach (var (enemyMove, _) in
                         SystemAPI.Query<RefRO<MoveData>, RefRO<AsteroidTag>>()
                             .WithNone<DeadTag>())
                {
                    var distSq = math.distancesq(missilePos, enemyMove.ValueRO.Position);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestPos = enemyMove.ValueRO.Position;
                        hasTarget = true;
                    }
                }

                foreach (var (enemyMove, _) in
                         SystemAPI.Query<RefRO<MoveData>, RefRO<UfoTag>>()
                             .WithNone<DeadTag>())
                {
                    var distSq = math.distancesq(missilePos, enemyMove.ValueRO.Position);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestPos = enemyMove.ValueRO.Position;
                        hasTarget = true;
                    }
                }

                foreach (var (enemyMove, _) in
                         SystemAPI.Query<RefRO<MoveData>, RefRO<UfoBigTag>>()
                             .WithNone<DeadTag>())
                {
                    var distSq = math.distancesq(missilePos, enemyMove.ValueRO.Position);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestPos = enemyMove.ValueRO.Position;
                        hasTarget = true;
                    }
                }

                if (!hasTarget)
                {
                    continue;
                }

                var currentDir = move.ValueRO.Direction;
                var desiredDir = math.normalizesafe(nearestPos - missilePos);

                if (math.lengthsq(desiredDir) < 0.001f)
                {
                    continue;
                }

                var currentAngle = math.atan2(currentDir.y, currentDir.x);
                var desiredAngle = math.atan2(desiredDir.y, desiredDir.x);

                var angleDiff = desiredAngle - currentAngle;

                // Нормализация угла в [-PI, PI]
                while (angleDiff > math.PI)
                {
                    angleDiff -= 2f * math.PI;
                }
                while (angleDiff < -math.PI)
                {
                    angleDiff += 2f * math.PI;
                }

                var maxTurn = math.radians(homing.ValueRO.TurnSpeed) * deltaTime;
                var actualTurn = math.clamp(angleDiff, -maxTurn, maxTurn);

                var newAngle = currentAngle + actualTurn;
                move.ValueRW.Direction = new float2(math.cos(newAngle), math.sin(newAngle));
            }
        }
    }
}
