using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    [UpdateAfter(typeof(EcsLifeTimeSystem))]
    public partial class EcsDeadByLifeTimeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (lifeTime, entity) in
                     SystemAPI.Query<RefRO<LifeTimeData>>()
                         .WithNone<DeadTag>()
                         .WithEntityAccess())
            {
                if (lifeTime.ValueRO.TimeRemaining <= 0f)
                {
                    ecb.AddComponent<DeadTag>(entity);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
