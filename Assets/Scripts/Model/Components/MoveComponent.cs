using System;
using SelStrom.Asteroids;
using UnityEngine;

namespace Model.Components
{
    public class MoveComponent : IModelComponent    
    {
        public event Action OnPositionChanged;

        private SelStrom.Asteroids.Model _model;

        public Vector2 Speed { get; set; }
        private Vector2 _position = Vector2.zero;

        public Vector2 Position
        {
            get => _position;
            set
            {
                if (_position == value)
                {
                    return;
                }

                _position = value;
                OnPositionChanged?.Invoke();
            }
        }

        public void Connect(SelStrom.Asteroids.Model model)
        {
            _model = model;
        }

        public void Update(float deltaTime)
        {
            var oldPosition = Position;
            var position = oldPosition + Speed * deltaTime;
            CorrectPositionWithinGameArea(ref position.x, _model.GameArea.x);
            CorrectPositionWithinGameArea(ref position.y, _model.GameArea.y);
            Position = position;
        }
        
        private static void CorrectPositionWithinGameArea(ref float position, float side)
        {
            if (position > side / 2)
            {
                position = -side + position;
            }

            if (position < -side / 2)
            {
                position = side - position;
            }
        }
    }
}