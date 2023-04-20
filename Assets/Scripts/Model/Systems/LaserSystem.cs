using Model.Components;

namespace SelStrom.Asteroids
{
    public class LaserSystem : BaseModelSystem<LaserComponent>
    {
        protected override void UpdateNode(LaserComponent node, float deltaTime)
        {
            if (node.CurrentShoots.Value < node.MaxShoots)
            {
                node.ReloadRemaining.Value -= deltaTime;
                if (node.ReloadRemaining.Value <= 0)
                {
                    node.ReloadRemaining.Value = node.UpdateDurationSec;
                    node.CurrentShoots.Value += 1;
                }
            }
            
            if (node.Shooting && node.CurrentShoots.Value > 0)
            {
                node.CurrentShoots.Value -= 1;
                node.OnShooting?.Invoke(node);
            }
            
            node.Shooting = false;
        }
    }
}