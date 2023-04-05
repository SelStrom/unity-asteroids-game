using System;
using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class AsteroidModel : IGameEntityModel
    {
        public event Action<AsteroidModel> OnDestroyed;
        
        public MoveComponent Move = new();
        private bool _isDead;
        public AsteroidData Data { get; private set; }
        public int Age { get; private set; }

        public void SetData(AsteroidData data, int age, Vector2 position, Vector2 direction, float speed)
        {
            Data = data;
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