using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Событие запуска ракеты. Накапливается в singleton-буфере, обрабатывается
    /// managed-кодом (<see cref="ShootEventProcessorSystem"/>) для создания визуала + entity.
    /// </summary>
    public struct RocketLaunchEvent : IBufferElementData
    {
        public float2 Position;
        public float2 Direction;
    }
}
