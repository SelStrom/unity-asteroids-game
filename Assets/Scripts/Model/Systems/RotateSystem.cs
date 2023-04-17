using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class RotateSystem : BaseModelSystem<RotateComponent>
    {
        protected override void UpdateNode(RotateComponent com, float deltaTime)
        {
            if (com.TargetDirection == 0)
            {
                return;
            }

            com.Rotation.Value =
                Quaternion.Euler(0, 0, RotateComponent.DegreePerSecond * deltaTime * com.TargetDirection) *
                com.Rotation.Value;
        }
    }
}