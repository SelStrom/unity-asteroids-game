using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsMoveSystem))]
    [UpdateBefore(typeof(EcsCollisionHandlerSystem))]
    public partial struct EcsRocketHomingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var deltaTime = SystemAPI.Time.DeltaTime;

            var targets = CollectLivingTargets(ref em);

            foreach (var (homing, move) in
                     SystemAPI.Query<RefRW<RocketHomingData>, RefRW<MoveData>>())
            {
                var current = homing.ValueRO.TargetEntity;
                if (!IsTargetAlive(ref em, current))
                {
                    homing.ValueRW.TargetEntity = FindClosestTarget(ref em, targets, move.ValueRO.Position);
                }

                var target = homing.ValueRO.TargetEntity;
                if (target == Entity.Null)
                {
                    continue;
                }

                var targetPosition = em.GetComponentData<MoveData>(target).Position;
                var toTarget = targetPosition - move.ValueRO.Position;
                var distance = math.length(toTarget);
                if (distance < 1e-5f)
                {
                    continue;
                }

                var desired = toTarget / distance;
                var direction = math.normalizesafe(move.ValueRO.Direction, desired);
                var dot = math.clamp(math.dot(direction, desired), -1f, 1f);
                var angle = math.acos(dot);
                var maxStep = homing.ValueRO.TurnRateRad * deltaTime;

                float2 newDirection;
                if (angle <= maxStep)
                {
                    newDirection = desired;
                }
                else
                {
                    var cross = direction.x * desired.y - direction.y * desired.x;
                    var step = cross >= 0f ? maxStep : -maxStep;
                    var cos = math.cos(step);
                    var sin = math.sin(step);
                    newDirection = new float2(
                        direction.x * cos - direction.y * sin,
                        direction.x * sin + direction.y * cos
                    );
                }

                move.ValueRW.Direction = newDirection;
                move.ValueRW.Speed = homing.ValueRO.Speed;
            }

            targets.Dispose();
        }

        private NativeList<Entity> CollectLivingTargets(ref EntityManager em)
        {
            var list = new NativeList<Entity>(Allocator.Temp);

            var query = em.CreateEntityQuery(
                new EntityQueryDesc
                {
                    Any = new ComponentType[]
                    {
                        ComponentType.ReadOnly<AsteroidTag>(),
                        ComponentType.ReadOnly<UfoTag>(),
                        ComponentType.ReadOnly<UfoBigTag>()
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<DeadTag>()
                    },
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<MoveData>()
                    }
                });

            using (var entities = query.ToEntityArray(Allocator.Temp))
            {
                for (var i = 0; i < entities.Length; i++)
                {
                    list.Add(entities[i]);
                }
            }

            return list;
        }

        private bool IsTargetAlive(ref EntityManager em, Entity entity)
        {
            if (entity == Entity.Null)
            {
                return false;
            }

            if (!em.Exists(entity))
            {
                return false;
            }

            if (em.HasComponent<DeadTag>(entity))
            {
                return false;
            }

            if (!em.HasComponent<MoveData>(entity))
            {
                return false;
            }

            return em.HasComponent<AsteroidTag>(entity)
                   || em.HasComponent<UfoTag>(entity)
                   || em.HasComponent<UfoBigTag>(entity);
        }

        private Entity FindClosestTarget(ref EntityManager em, NativeList<Entity> targets, float2 origin)
        {
            var best = Entity.Null;
            var bestSqr = float.MaxValue;

            for (var i = 0; i < targets.Length; i++)
            {
                var candidate = targets[i];
                var pos = em.GetComponentData<MoveData>(candidate).Position;
                var sqr = math.lengthsq(pos - origin);
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
