using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public struct ScoreData : IComponentData
    {
        public int Value;
    }

    public struct ScoreValue : IComponentData
    {
        public int Score;
    }
}
