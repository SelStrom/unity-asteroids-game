using SelStrom.Asteroids;

namespace Model.Components
{
    public class MoveToComponent : IModelComponent
    {
        public ShipModel Ship;

        private float _every;
        public float Every
        {
            get => _every;
            set
            {
                _every = value;
                ReadyRemaining = value;
            }
        }

        public float ReadyRemaining;
    }
}