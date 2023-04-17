using System;
using Model.Components;

namespace SelStrom.Asteroids
{
    public class LifeTimeSystem : BaseModelSystem<LifeTimeComponent>
    {
        protected override void UpdateNode(LifeTimeComponent com, float deltaTime)
        {
            com.TimeRemaining = Math.Max(com.TimeRemaining - deltaTime, 0);
        }
    }
}