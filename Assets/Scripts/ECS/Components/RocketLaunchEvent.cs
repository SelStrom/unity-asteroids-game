using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Событие пуска ракеты. Накапливается в singleton-буфере и обрабатывается
    /// в ShootEventProcessorSystem (создание визуала + снаряда-ракеты).
    /// </summary>
    public struct RocketLaunchEvent : IBufferElementData
    {
        public float2 Position;
        public float2 Direction;
    }
}
