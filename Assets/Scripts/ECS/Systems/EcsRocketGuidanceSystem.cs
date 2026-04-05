using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsMoveSystem))]
    public partial class EcsRocketGuidanceSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }
    }
}
