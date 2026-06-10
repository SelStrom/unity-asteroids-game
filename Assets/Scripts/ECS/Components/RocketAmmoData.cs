using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketAmmoData : IComponentData
    {
        public int MaxRockets;
        public float RespawnDurationSec;
        public int CurrentRockets;
        public float RespawnRemaining;
        public bool Launching;
        public float2 Direction;
        public float2 LaunchPosition;
    }
}
