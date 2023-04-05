using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class BulletModel : IGameEntityModel
    {
        public MoveComponent Move = new();
        private bool _isDead;
        private float _lifeTime;

        public void SetData(GameData.BulletData data, Vector2 position, Vector2 direction)
        {
            _lifeTime = data.LifeTimeSeconds;
            Move.Position.Value = position;
            Move.Direction = direction;
            Move.Speed = data.Speed;
        }

        public bool IsDead() => _isDead;
        
        public void Connect(Model model)
        {
            Move.Connect(model);
        }

        public void Update(float deltaTime)
        {
            Move.Update(deltaTime);

            _lifeTime -= deltaTime;
            if (_lifeTime <= 0)
            {
                Kill();
            }
        }

        public void Kill()
        {
            _isDead = true;
            
        }
    }
}