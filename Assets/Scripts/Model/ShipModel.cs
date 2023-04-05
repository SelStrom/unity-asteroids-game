using System;
using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ShipModel : IGameEntityModel
    {
        private const float UnitsPerSecond = 6.0f;
        private const float MaxSpeed = 15.0f;
        private const float MinSpeed = 0.0f;
        private const int DegreePerSecond = 90;
        
        public float RotationDirection { get; set; }

        public MoveComponent Move = new();
        public readonly ObservableField<bool> Thrust = new();
        public readonly ObservableField<Vector2> Rotation = new(Vector2.right);
        
        private bool _isDead;
        public bool IsDead() => _isDead;
        public void Kill()
        {
            _isDead = true;            
        }

        public void Connect(Model model)
        {
            Move.Connect(model);
        }

        public void Update(float deltaTime)
        {
            if (RotationDirection != 0)
            {
                Rotation.Value = Quaternion.Euler(0, 0, DegreePerSecond * deltaTime * RotationDirection) * Rotation.Value;
            }

            if (Thrust.Value)
            {
                var acceleration = UnitsPerSecond * deltaTime;
                var velocity = Move.Direction * Move.Speed + Rotation.Value * acceleration;
                
                Move.Direction = velocity.normalized;
                Move.Speed = Math.Min(velocity.magnitude, MaxSpeed);
            }
            else
            {
                Move.Speed = Math.Max(Move.Speed - UnitsPerSecond / 2 * deltaTime, MinSpeed);
            }

            Move.Update(deltaTime);
        }
    }
}