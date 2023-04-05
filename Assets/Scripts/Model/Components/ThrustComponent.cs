using SelStrom.Asteroids;

namespace Model.Components
{
    public class ThrustComponent : IModelComponent
    {
        public const float UnitsPerSecond = 6.0f;
        public const float MaxSpeed = 15.0f;
        public const float MinSpeed = 0.0f;
        
        public readonly ObservableField<bool> IsActive = new();
    }
}