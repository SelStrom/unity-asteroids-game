using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class BulletModel : IGameEntity
    {
        private bool _isDead;
        
        public MoveComponent Move = new();
        private float _lifeTime;

        public BulletModel(int lifeTimeSeconds, Vector2 position, Vector2 direction)
        {
            _lifeTime = lifeTimeSeconds;
            Move.Position = position;
            Move.Speed = direction * 20;
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
                _isDead = true;
            }
        }
    }
}