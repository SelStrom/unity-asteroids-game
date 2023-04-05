using System;
using SelStrom.Asteroids;
using UnityEngine;

namespace Model.Components
{
    public class MoveComponent : IModelComponent    
    {
        private SelStrom.Asteroids.Model _model;

        public readonly ObservableField<Vector2> Position = new();
        public float Speed { get; set; }
        public Vector2 Direction { get; set; }
        
        public void Connect(SelStrom.Asteroids.Model model)
        {
            _model = model;
        }

        public void Update(float deltaTime)
        {
            var oldPosition = Position.Value;
            var position = oldPosition + Direction * (Speed * deltaTime);
            PlaceWithinGameArea(ref position.x, _model.GameArea.x);
            PlaceWithinGameArea(ref position.y, _model.GameArea.y);
            Position.Value = position;
        }
        
        private static void PlaceWithinGameArea(ref float position, float side)
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