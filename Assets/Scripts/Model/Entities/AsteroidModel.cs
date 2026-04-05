using System;
using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class AsteroidModel : IGameEntityModel
    {
        public MoveComponent Move { get; private set; } = new();
        public AsteroidData Data { get; private set; }
        public int Age { get; private set; }

        private bool _killed;

        public void AcceptWith(IGroupVisitor visitor)
        {
            visitor.Visit(this);
        }
        
        public void SetData(AsteroidData data, int age, Vector2 position, Vector2 direction, float speed)
        {
            Data = data;
            Age = age;
            Move.Position.Value = position;
            Move.Direction = direction;
            Move.Speed.Value = speed;
        }

        public bool IsDead() => _killed;

        public void Kill()
        {
            _killed = true;
        }
    }
}