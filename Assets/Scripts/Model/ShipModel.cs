using System;
using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    
    
    public class ShipModel : IGameEntity
    {
        public float RotationDirection { get; set; }

        public MoveComponent Move = new();
        public readonly ObservableValue<bool> Thrust = new();
        public readonly ObservableValue<Vector2> Rotation = new(Vector2.right);

        public bool IsDead() => false;

        public void Connect(Model model)
        {
            Move.Connect(model);
        }

        public void Update(float deltaTime)
        {
            if (RotationDirection != 0)
            {
                const int degreePerSecond = 90;
                Rotation.Value = Quaternion.Euler(0, 0, degreePerSecond * deltaTime * RotationDirection) * Rotation.Value;
            }

            if (Thrust.Value)
            {
                const float unitsPerSecond = 6.0f;
                Move.Speed += Rotation.Value * (unitsPerSecond * deltaTime);
            }

            Move.Update(deltaTime);
        }
    }
}