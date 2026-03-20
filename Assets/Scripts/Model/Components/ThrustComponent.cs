using Shtl.Mvvm;

namespace Model.Components
{
    public class ThrustComponent : IModelComponent
    {
        public const float MinSpeed = 0.0f;

        public float UnitsPerSecond;
        public float MaxSpeed;
        public readonly ObservableValue<bool> IsActive = new();
    }
}