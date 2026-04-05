using Model.Components;

namespace SelStrom.Asteroids
{
    public class GunSystem : BaseModelSystem<GunComponent>
    {
        protected override void UpdateNode(GunComponent node, float deltaTime)
        {
            if (node.CurrentShoots < node.MaxShoots)
            {
                node.ReloadRemaining -= deltaTime;
                if (node.ReloadRemaining <= 0)
                {
                    node.ReloadRemaining = node.ReloadDurationSec;
                    node.CurrentShoots = node.MaxShoots;
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