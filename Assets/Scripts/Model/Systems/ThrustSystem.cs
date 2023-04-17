using System;
using Model.Components;

namespace SelStrom.Asteroids
{
    public class ThrustSystem : BaseModelSystem<(ThrustComponent Thrust, MoveComponent Move, RotateComponent Rotate)>
    {
        protected override void UpdateNode((ThrustComponent Thrust, MoveComponent Move, RotateComponent Rotate) com, float deltaTime)
        {
            if (com.Thrust.IsActive.Value)
            {
                var acceleration = com.Thrust.UnitsPerSecond * deltaTime;
                var velocity = com.Move.Direction * com.Move.Speed + com.Rotate.Rotation.Value * acceleration;

                com.Move.Direction = velocity.normalized;
                com.Move.Speed = Math.Min(velocity.magnitude, com.Thrust.MaxSpeed);
            }
            else
            {
                com.Move.Speed = Math.Max(com.Move.Speed - com.Thrust.UnitsPerSecond / 2 * deltaTime,
                    ThrustComponent.MinSpeed);
            }
        }
    }
}