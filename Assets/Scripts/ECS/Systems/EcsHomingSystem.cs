using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsMissileSystem))]
    public partial struct EcsHomingSystem : ISystem
    {
        private EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            _enemyQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<AsteroidTag, UfoTag, UfoBigTag>()
                .WithAll<MoveData>()
                .WithNone<DeadTag>()
                .Build(ref state);

            state.RequireForUpdate<HomingData>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Кэшируем кандидатов на цель один раз за кадр.
            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.Temp);
            var enemyMoves = _enemyQuery.ToComponentDataArray<MoveData>(Allocator.Temp);

            foreach (var (homing, move, rotate) in
                     SystemAPI.Query<RefRW<HomingData>, RefRW<MoveData>, RefRW<RotateData>>()
                         .WithAll<MissileTag>())
            {
                var target = homing.ValueRO.TargetEntity;

                if (target != Entity.Null)
                {
                    if (!state.EntityManager.Exists(target)
                        || state.EntityManager.HasComponent<DeadTag>(target))
                    {
                        target = Entity.Null;
                        homing.ValueRW.TargetEntity = Entity.Null;
                    }
                }

                if (target == Entity.Null)
                {
                    target = FindNearestEnemy(
                        move.ValueRO.Position,
                        homing.ValueRO.TargetAcquisitionRange,
                        enemyEntities,
                        enemyMoves);
                    homing.ValueRW.TargetEntity = target;
                }

                if (target != Entity.Null)
                {
                    var targetPos = state.EntityManager.GetComponentData<MoveData>(target).Position;
                    SteerToward(ref move.ValueRW, targetPos,
                        homing.ValueRO.TurnRateRadPerSec, deltaTime);
                }

                // Спрайт ракеты ориентируем по фактическому направлению полёта
                // (даже если цели нет — ракета продолжает лететь по последнему курсу).
                rotate.ValueRW.Rotation = move.ValueRO.Direction;
            }

            enemyEntities.Dispose();
            enemyMoves.Dispose();
        }

        private static Entity FindNearestEnemy(
            float2 from,
            float maxRange,
            NativeArray<Entity> entities,
            NativeArray<MoveData> moves)
        {
            var bestEntity = Entity.Null;
            var bestDistSq = maxRange * maxRange;

            for (int i = 0; i < entities.Length; i++)
            {
                var distSq = math.distancesq(moves[i].Position, from);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestEntity = entities[i];
                }
            }

            return bestEntity;
        }

        private static void SteerToward(
            ref MoveData move,
            float2 targetPos,
            float turnRateRadPerSec,
            float deltaTime)
        {
            var toTarget = targetPos - move.Position;
            var dist = math.length(toTarget);
            if (dist < 1e-5f)
            {
                return;
            }

            var desired = toTarget / dist;
            var current = move.Direction;
            if (math.lengthsq(current) < 1e-10f)
            {
                move.Direction = desired;
                return;
            }

            var currentAngle = math.atan2(current.y, current.x);
            var desiredAngle = math.atan2(desired.y, desired.x);

            // Нормируем разницу в диапазон [-PI, PI], чтобы выбирать кратчайший поворот.
            var deltaAngle = desiredAngle - currentAngle;
            deltaAngle = math.fmod(deltaAngle + math.PI, 2f * math.PI);
            if (deltaAngle < 0f)
            {
                deltaAngle += 2f * math.PI;
            }
            deltaAngle -= math.PI;

            var maxTurn = turnRateRadPerSec * deltaTime;
            var clamped = math.clamp(deltaAngle, -maxTurn, maxTurn);
            var newAngle = currentAngle + clamped;
            move.Direction = new float2(math.cos(newAngle), math.sin(newAngle));
        }
    }
}
