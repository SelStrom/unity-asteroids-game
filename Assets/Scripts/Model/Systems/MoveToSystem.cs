using Model.Components;

namespace SelStrom.Asteroids
{
    public class MoveToSystem : BaseModelSystem<(MoveComponent Move, MoveToComponent MoveTo)>
    {
        protected override void UpdateNode((MoveComponent Move, MoveToComponent MoveTo) node, float deltaTime)
        {
            node.MoveTo.ReadyRemaining -= deltaTime;
            if (node.MoveTo.ReadyRemaining > 0)
            {
                return;
            }

            node.MoveTo.ReadyRemaining = node.MoveTo.Every;

            var ship = node.MoveTo.Ship;
            var time = (ship.Move.Position.Value - node.Move.Position.Value).magnitude
                       / (node.Move.Speed.Value - ship.Move.Speed.Value);

            var pendingPosition = ship.Move.Position.Value + (ship.Move.Direction * ship.Move.Speed.Value) * time;
            node.Move.Direction = (pendingPosition - node.Move.Position.Value).normalized;
        }
    }
}