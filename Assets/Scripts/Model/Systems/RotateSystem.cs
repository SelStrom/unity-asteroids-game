using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class RotateSystem : BaseModelSystem<RotateComponent>
    {
        protected override void UpdateNode(RotateComponent node, float deltaTime)
        {
            if (node.TargetDirection == 0)
            {
                return;
            }

            node.Rotation.Value =
                Quaternion.Euler(0, 0, RotateComponent.DegreePerSecond * deltaTime * node.TargetDirection) *
                node.Rotation.Value;
        }
    }
}