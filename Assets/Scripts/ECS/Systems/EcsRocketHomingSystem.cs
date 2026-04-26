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

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var em = state.EntityManager;

            foreach (var (move, homing) in
                     SystemAPI.Query<RefRW<MoveData>, RefRO<RocketHomingData>>()
                         .WithAll<RocketTag>())
            {
                var target = homing.ValueRO.TargetEntity;
                if (target == Entity.Null || !em.Exists(target))
                {
                    continue;
                }

                if (!em.HasComponent<MoveData>(target))
                {
                    continue;
                }

                if (em.HasComponent<DeadTag>(target))
                {
                    continue;
                }

                var targetPos = em.GetComponentData<MoveData>(target).Position;
                var currentDir = move.ValueRO.Direction;
                if (math.lengthsq(currentDir) <= 0f)
                {
                    continue;
                }
                currentDir = math.normalize(currentDir);

                var toTarget = targetPos - move.ValueRO.Position;
                if (math.lengthsq(toTarget) <= 0f)
                {
                    continue;
                }
                var desiredDir = math.normalize(toTarget);

                // signed angle между currentDir и desiredDir, в радианах, [-PI, PI]
                var crossZ = currentDir.x * desiredDir.y - currentDir.y * desiredDir.x;
                var dot = math.dot(currentDir, desiredDir);
                var angle = math.atan2(crossZ, dot);

                var maxTurn = homing.ValueRO.TurnRateRadPerSec * deltaTime;
                var clamped = math.clamp(angle, -maxTurn, maxTurn);

                var cosA = math.cos(clamped);
                var sinA = math.sin(clamped);
                var newDir = new float2(
                    currentDir.x * cosA - currentDir.y * sinA,
                    currentDir.x * sinA + currentDir.y * cosA);

                move.ValueRW.Direction = newDir;
            }
        }
    }
}
