using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsMoveSystem))]
    [UpdateBefore(typeof(EcsLifeTimeSystem))]
    public partial struct EcsShipPositionUpdateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShipPositionData>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var move in SystemAPI.Query<RefRO<MoveData>>().WithAll<ShipTag>())
            {
                SystemAPI.SetSingleton(new ShipPositionData
                {
                    Position = move.ValueRO.Position,
                    Speed = move.ValueRO.Speed,
                    Direction = move.ValueRO.Direction
                });
            }
        }
    }
}
