using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public struct RocketLauncherData : IComponentData
    {
        public int MaxRockets;
        public int CurrentRockets;
        public float RespawnDurationSec;
        public float RespawnRemaining;
        public bool Launching;
        public float2 LaunchPosition;
        public float2 LaunchDirection;
    }
}
