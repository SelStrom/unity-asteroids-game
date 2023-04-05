using System;
using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class AsteroidModel : IGameEntityModel
    {
        public event Action<AsteroidModel> OnDestroyed;
        
        public MoveComponent Move = new();
        private bool _isDead;
        public int Age { get; private set; }

        public void SetData(int age, Vector2 position, Vector2 direction, float speed)
        {
            Age = age;
            Move.Position.Value = position;
            Move.Direction = direction;
            Move.Speed = speed;
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

        public void Kill()
        {
            _isDead = true;
        }
    }
}