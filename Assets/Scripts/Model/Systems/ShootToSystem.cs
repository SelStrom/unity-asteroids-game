using Model.Components;

namespace SelStrom.Asteroids
{
    public class ShootToSystem : BaseModelSystem<(MoveComponent Move, GunComponent Gun, ShootToComponent ShootTo)>
    {
        protected override void UpdateNode((MoveComponent Move, GunComponent Gun, ShootToComponent ShootTo) node,
            float deltaTime)
        {
            if (node.Gun.CurrentShoots <= 0)
            {
                return;
            }

            var ship = node.ShootTo.Ship;
            var time = (ship.Move.Position.Value - node.Move.Position.Value).magnitude
                       / (20 - ship.Move.Speed.Value);

            var pendingPosition = ship.Move.Position.Value + (ship.Move.Direction * ship.Move.Speed.Value) * time;
            var direction = (pendingPosition - node.Move.Position.Value).normalized;

            node.Gun.Shooting = true;
            node.Gun.Direction = direction;
            node.Gun.ShootPosition = node.Move.Position.Value;
        }
    }
}