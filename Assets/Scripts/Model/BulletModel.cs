using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class AsteroidModel : IGameEntity
    {
        public MoveComponent Move = new();
        private bool _isDead;
        public int Age { get; private set; }

        public AsteroidModel(int age, Vector2 position, Vector2 direction, float speed)
        {
            Age = age;
            Move.Position.Value = position;
            Move.Speed = direction * speed;
        }

        public bool IsDead() => _isDead;
        
        public void Connect(Model model)
        {
            Move.Connect(model);
        }

        public void Update(float deltaTime)
        {
            Move.Update(deltaTime);
        }
    }
    
    public class BulletModel : IGameEntity
    {
        public MoveComponent Move = new();
        private bool _isDead;
        private float _lifeTime;

        public BulletModel(int lifeTimeSeconds, Vector2 position, Vector2 direction, float speed)
        {
            _lifeTime = lifeTimeSeconds;
            Move.Position.Value = position;
            Move.Speed = direction * speed;
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