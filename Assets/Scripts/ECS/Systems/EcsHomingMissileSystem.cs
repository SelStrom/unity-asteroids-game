using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateBefore(typeof(EcsMoveSystem))]
    public partial struct EcsHomingMissileSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;

            using var asteroidQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<AsteroidTag>(),
                ComponentType.ReadOnly<MoveData>());
            using var ufoQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UfoTag>(),
                ComponentType.ReadOnly<MoveData>());
            using var ufoBigQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UfoBigTag>(),
                ComponentType.ReadOnly<MoveData>());

            foreach (var (move, rotate, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<RotateData>, RefRO<HomingMissileData>>()
                         .WithAll<MissileTag>())
            {
                var missilePos = move.ValueRO.Position;
                var seekRangeSq = homing.ValueRO.SeekRange * homing.ValueRO.SeekRange;

                var found = false;
                var bestDistSq = seekRangeSq;
                var bestPos = default(float2);

                ScanQuery(em, asteroidQuery, missilePos, ref bestDistSq, ref bestPos, ref found);
                ScanQuery(em, ufoQuery, missilePos, ref bestDistSq, ref bestPos, ref found);
                ScanQuery(em, ufoBigQuery, missilePos, ref bestDistSq, ref bestPos, ref found);

                if (!found)
                {
                    rotate.ValueRW.Rotation = math.normalizesafe(move.ValueRO.Direction);
                    continue;
                }

                var desiredDir = math.normalizesafe(bestPos - missilePos);
                if (math.lengthsq(desiredDir) < 1e-8f)
                {
                    rotate.ValueRW.Rotation = math.normalizesafe(move.ValueRO.Direction);
                    continue;
                }

                var currentDir = move.ValueRO.Direction;
                if (math.lengthsq(currentDir) < 1e-8f)
                {
                    move.ValueRW.Direction = desiredDir;
                    rotate.ValueRW.Rotation = desiredDir;
                    continue;
                }

                currentDir = math.normalize(currentDir);

                var maxStep = homing.ValueRO.TurnRateRadPerSec * deltaTime;
                var newDir = RotateTowards(currentDir, desiredDir, maxStep);
                move.ValueRW.Direction = newDir;
                rotate.ValueRW.Rotation = newDir;
            }
        }

        private static void ScanQuery(EntityManager em, EntityQuery query,
            float2 missilePos, ref float bestDistSq, ref float2 bestPos, ref bool found)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (em.HasComponent<DeadTag>(e))
                {
                    continue;
                }

                var pos = em.GetComponentData<MoveData>(e).Position;
                var distSq = math.lengthsq(pos - missilePos);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestPos = pos;
                    found = true;
                }
            }

            entities.Dispose();
        }

        private static float2 RotateTowards(float2 from, float2 to, float maxStepRad)
        {
            var dot = math.clamp(math.dot(from, to), -1f, 1f);
            var angle = math.acos(dot);
            if (angle <= maxStepRad)
            {
                return to;
            }

            var cross = from.x * to.y - from.y * to.x;
            var sign = cross >= 0f ? 1f : -1f;

            var cos = math.cos(maxStepRad);
            var sin = math.sin(maxStepRad) * sign;

            return new float2(
                from.x * cos - from.y * sin,
                from.x * sin + from.y * cos);
        }
    }
}
