using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    public partial struct EcsMoveToSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShipPositionData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var shipPos = SystemAPI.GetSingleton<ShipPositionData>();
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (move, moveTo) in
                     SystemAPI.Query<RefRW<MoveData>, RefRW<MoveToData>>())
            {
                moveTo.ValueRW.ReadyRemaining -= deltaTime;
                if (moveTo.ValueRO.ReadyRemaining > 0)
                {
                    continue;
                }

                moveTo.ValueRW.ReadyRemaining = moveTo.ValueRO.Every;

                var distance = math.length(shipPos.Position - move.ValueRO.Position);
                var time = distance / (move.ValueRO.Speed - shipPos.Speed);
                var pendingPosition = shipPos.Position
                                      + (shipPos.Direction * shipPos.Speed) * time;
                move.ValueRW.Direction = math.normalizesafe(pendingPosition - move.ValueRO.Position);
            }
        }
    }
}
