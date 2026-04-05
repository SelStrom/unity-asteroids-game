using System;
using Model.Components;

namespace SelStrom.Asteroids
{
    public class ThrustSystem : BaseModelSystem<(ThrustComponent Thrust, MoveComponent Move, RotateComponent Rotate)>
    {
        protected override void UpdateNode((ThrustComponent Thrust, MoveComponent Move, RotateComponent Rotate) node, float deltaTime)
        {
            if (node.Thrust.IsActive.Value)
            {
                var acceleration = node.Thrust.UnitsPerSecond * deltaTime;
                var velocity = node.Move.Direction * node.Move.Speed.Value + node.Rotate.Rotation.Value * acceleration;

                node.Move.Direction = velocity.normalized;
                node.Move.Speed.Value = Math.Min(velocity.magnitude, node.Thrust.MaxSpeed);
            }
            else
            {
                node.Move.Speed.Value = Math.Max(node.Move.Speed.Value - node.Thrust.UnitsPerSecond / 2 * deltaTime,
                    ThrustComponent.MinSpeed);
            }
        }
    }
}