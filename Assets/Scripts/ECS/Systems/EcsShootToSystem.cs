using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsShipPositionUpdateSystem))]
    public partial struct EcsShootToSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShipPositionData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var shipPos = SystemAPI.GetSingleton<ShipPositionData>();

            foreach (var (move, gun, shootTo) in
                     SystemAPI.Query<RefRO<MoveData>, RefRW<GunData>, RefRO<ShootToData>>())
            {
                if (gun.ValueRO.CurrentShoots <= 0)
                {
                    continue;
                }

                var distance = math.length(shipPos.Position - move.ValueRO.Position);
                var time = distance / (20f - shipPos.Speed);
                var pendingPosition = shipPos.Position
                                      + (shipPos.Direction * shipPos.Speed) * time;
                var direction = math.normalizesafe(pendingPosition - move.ValueRO.Position);

                gun.ValueRW.Shooting = true;
                gun.ValueRW.Direction = direction;
                gun.ValueRW.ShootPosition = move.ValueRO.Position;
            }
        }
    }
}
