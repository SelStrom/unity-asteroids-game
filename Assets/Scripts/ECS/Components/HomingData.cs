using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    // Компонент летящей ракеты. Хранит ссылку на текущую цель
    // и параметры наведения. EcsHomingSystem каждый кадр поворачивает
    // MoveData.Direction к цели на угол, ограниченный TurnRateRadPerSec.
    public struct HomingData : IComponentData
    {
        public Entity TargetEntity;
        public float TurnRateRadPerSec;
        public float TargetAcquisitionRange;
    }
}
