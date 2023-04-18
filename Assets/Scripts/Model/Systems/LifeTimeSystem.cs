using System;
using Model.Components;

namespace SelStrom.Asteroids
{
    public class LifeTimeSystem : BaseModelSystem<LifeTimeComponent>
    {
        protected override void UpdateNode(LifeTimeComponent node, float deltaTime)
        {
            node.TimeRemaining = Math.Max(node.TimeRemaining - deltaTime, 0);
        }
    }
}