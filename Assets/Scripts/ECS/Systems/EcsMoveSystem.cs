using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.ECS
{
    [BurstCompile]
    [UpdateAfter(typeof(EcsThrustSystem))]
    [UpdateBefore(typeof(EcsShipPositionUpdateSystem))]
    public partial struct EcsMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameAreaData>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var gameArea = SystemAPI.GetSingleton<GameAreaData>();

            foreach (var move in SystemAPI.Query<RefRW<MoveData>>())
            {
                var position = move.ValueRO.Position +
                               move.ValueRO.Direction * (move.ValueRO.Speed * deltaTime);
                PlaceWithinGameArea(ref position.x, gameArea.Size.x);
                PlaceWithinGameArea(ref position.y, gameArea.Size.y);
                move.ValueRW.Position = position;
            }
        }

        private static void PlaceWithinGameArea(ref float position, float side)
        {
            if (position > side / 2)
            {
                position = -side + position;
            }

            if (position < -side / 2)
            {
                position = side - position;
            }
        }
    }
}
