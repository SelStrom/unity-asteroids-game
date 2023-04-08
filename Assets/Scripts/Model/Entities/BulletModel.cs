using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class BulletModel : IGameEntityModel
    {
        public LifeTimeComponent LifeTime = new();
        public MoveComponent Move = new();
        
        private bool _killed;

        public void ConnectWith(IGroupHolder groupHolder)
        {
            groupHolder.Group(this);
        }

        public void SetData(GameData.BulletData data, Vector2 position, Vector2 direction, float speed)
        {
            LifeTime.TimeRemaining = data.LifeTimeSeconds;
            Move.Position.Value = position;
            Move.Direction = direction;
            Move.Speed = speed;
        }

        public bool IsDead() => LifeTime.TimeRemaining <= 0 || _killed;

        public void Kill()
        {
            _killed = true;
        }
    }
}