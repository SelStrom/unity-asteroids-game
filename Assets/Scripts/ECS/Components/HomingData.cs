using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Параметры самонаведения снаряда. Ограниченная скорость поворота
    /// (TurnRateDegPerSec) заставляет ракету лететь по дуге к цели.
    /// </summary>
    public struct HomingData : IComponentData
    {
        public float TurnRateDegPerSec;
    }
}
