using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    /// <summary>
    /// Пусковая установка ракет на корабле. Перезарядка инкрементальная (+1 ракета за период),
    /// аналогично <see cref="LaserData"/>: после запуска счётчик респавна начинает тикать.
    /// </summary>
    public struct RocketLauncherData : IComponentData
    {
        public int MaxRockets;
        public float ReloadDurationSec;
        public int CurrentRockets;
        public float ReloadRemaining;
        public bool Launching;
        public float2 LaunchPosition;
        public float2 LaunchDirection;
    }
}
