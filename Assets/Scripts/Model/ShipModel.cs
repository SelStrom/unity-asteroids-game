using System;
using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ShipModel : IGameEntity
    {
        public event Action OnRotationChanged;

        public float RotationDirection { get; set; }
        public bool IsAccelerated { get; set; }
        public MoveComponent Move = new();
        
        private Vector2 _rotation = Vector2.right;
        public Vector2 Rotation
        {
            get => _rotation;
            private set
            {
                if (_rotation == value)
                {
                    return;
                }

                _rotation = value;
                OnRotationChanged?.Invoke();
            }
        }

        public bool IsDead() => false;
        
        public void Connect(Model model)
        {
            Move.Connect(model);
        }

        public void Update(float deltaTime)
        {
            if (RotationDirection != 0)
            {
                const int angleVelocity = 90;
                Rotation = Quaternion.Euler(0,0,angleVelocity * deltaTime * RotationDirection) * Rotation;
            }

            if (IsAccelerated)
            {
                const float acceleration = 6.0f;
                Move.Speed += Rotation * (acceleration * deltaTime);
            }

            Move.Update(deltaTime);
        }
    }
}