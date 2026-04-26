using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    // Компонент-«пушка» ракет, висит на корабле. Управляет лимитом ракет
    // и таймером инкрементального респавна (по аналогии с LaserData).
    public struct MissileData : IComponentData
    {
        public int MaxShoots;
        public float RespawnDurationSec;
        public int CurrentShoots;
        public float RespawnRemaining;
        public bool Shooting;
        public float2 Direction;
        public float2 ShootPosition;
    }
}
