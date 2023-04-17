using Model.Components;

namespace SelStrom.Asteroids
{
    public class MoveToSystem : BaseModelSystem<(MoveComponent Move, MoveToComponent MoveTo)>
    {
        protected override void UpdateNode((MoveComponent Move, MoveToComponent MoveTo) com, float deltaTime)
        {
            com.MoveTo.ReadyRemaining -= deltaTime;
            if (com.MoveTo.ReadyRemaining > 0)
            {
                return;
            }

            com.MoveTo.ReadyRemaining = com.MoveTo.Every;

            var ship = com.MoveTo.Ship;
            var time = (ship.Move.Position.Value - com.Move.Position.Value).magnitude
                       / (com.Move.Speed - ship.Move.Speed);

            var pendingPosition = ship.Move.Position.Value + (ship.Move.Direction * ship.Move.Speed) * time;
            com.Move.Direction = (pendingPosition - com.Move.Position.Value).normalized;
        }
    }
}