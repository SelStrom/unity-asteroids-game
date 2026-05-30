using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Параметры самонаведения ракеты. Ограничение скорости поворота даёт траекторию-дугу:
    /// за кадр направление движения может измениться не более чем на TurnRateDegPerSec * dt.
    /// </summary>
    public struct HomingData : IComponentData
    {
        public float TurnRateDegPerSec;
    }
}
