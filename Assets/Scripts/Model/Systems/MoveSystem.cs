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

        protected override void UpdateNode(MoveComponent node, float deltaTime)
        {
            var oldPosition = node.Position.Value;
            var position = oldPosition + node.Direction * (node.Speed.Value * deltaTime);
            Model.PlaceWithinGameArea(ref position.x, _owner.GameArea.x);
            Model.PlaceWithinGameArea(ref position.y, _owner.GameArea.y);
            node.Position.Value = position;
        }
    }
}