using Model.Components;

namespace SelStrom.Asteroids
{
    public class ShootToSystem : BaseModelSystem<(MoveComponent Move, ShootToComponent ShootTo)>
    {
        private Model _owner;

        public void Connect(Model model)
        {
            _owner = model;
        }
        
        protected override void UpdateNode((MoveComponent Move, ShootToComponent ShootTo) com, float deltaTime)
        {
            com.ShootTo.ReadyRemaining -= deltaTime;
            if (com.ShootTo.ReadyRemaining > 0)
            {
                return;
            }

            com.ShootTo.ReadyRemaining = com.ShootTo.Every;

            var ship = com.ShootTo.Ship;
            var time = (ship.Move.Position.Value - com.Move.Position.Value).magnitude
                       / (20 - ship.Move.Speed);

            var pendingPosition = ship.Move.Position.Value + (ship.Move.Direction * ship.Move.Speed) * time;
            var direction = (pendingPosition - com.Move.Position.Value).normalized;

            _owner.OnShootReady?.Invoke(com.Move.Position.Value, direction);
        }
    }
}