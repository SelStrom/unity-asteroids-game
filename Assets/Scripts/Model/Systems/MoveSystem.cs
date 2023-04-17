using Model.Components;

namespace SelStrom.Asteroids
{
    public class MoveSystem : BaseModelSystem<MoveComponent>
    {
        private Model _owner;

        public void Connect(Model model)
        {
            _owner = model;
        }

        protected override void UpdateNode(MoveComponent com, float deltaTime)
        {
            var oldPosition = com.Position.Value;
            var position = oldPosition + com.Direction * (com.Speed * deltaTime);
            Model.PlaceWithinGameArea(ref position.x, _owner.GameArea.x);
            Model.PlaceWithinGameArea(ref position.y, _owner.GameArea.y);
            com.Position.Value = position;
        }
    }
}