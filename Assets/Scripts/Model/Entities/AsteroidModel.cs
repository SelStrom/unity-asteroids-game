using System;
using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class AsteroidModel : IGameEntityModel
    {
        public AsteroidData Data { get; private set; }
        public MoveComponent Move { get; private set; } = new();
        
        private bool _killed;
        public int Age { get; private set; }

        public void ConnectWith(IGroupHolder groupHolder)
        {
            groupHolder.Group(this);
        }

        public void SetData(AsteroidData data, int age, Vector2 position, Vector2 direction, float speed)
        {
            Data = data;
            Age = age;
            Move.Position.Value = position;
            Move.Direction = direction;
            Move.Speed = speed;
        }

        public bool IsDead() => _killed;

        public void Kill()
        {
            _killed = true;
        }
    }
}