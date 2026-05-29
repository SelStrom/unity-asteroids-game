using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Данные самонаводящейся ракеты: скорость поворота (даёт дугу) и текущая цель.
    /// </summary>
    public struct RocketData : IComponentData
    {
        public float TurnRateDegPerSec;
        public Entity Target;
    }
}
