using Model.Components;

namespace SelStrom.Asteroids
{
    public class LaserSystem : BaseModelSystem<LaserComponent>
    {
        protected override void UpdateNode(LaserComponent node, float deltaTime)
        {
            if (node.CurrentShoots < node.MaxShoots)
            {
                node.ReloadRemaining -= deltaTime;
                if (node.ReloadRemaining <= 0)
                {
                    node.ReloadRemaining = node.UpdateDurationSec;
                    node.CurrentShoots++;
                }
            }
            
            if (node.Shooting && node.CurrentShoots > 0)
            {
                node.CurrentShoots--;
                node.OnShooting?.Invoke(node);
            }
            
            node.Shooting = false;
        }
    }
}