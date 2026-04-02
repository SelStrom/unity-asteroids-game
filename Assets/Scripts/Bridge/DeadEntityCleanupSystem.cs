using System;
using SelStrom.Asteroids.ECS;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class DeadEntityCleanupSystem : SystemBase
    {
        private Action<GameObject> _onDeadEntity;

        public void SetOnDeadEntityCallback(Action<GameObject> callback)
        {
            _onDeadEntity = callback;
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (goRef, entity) in
                     SystemAPI.Query<GameObjectRef>()
                         .WithAll<DeadTag>()
                         .WithEntityAccess())
            {
                _onDeadEntity?.Invoke(goRef.GameObject);
                ecb.RemoveComponent<GameObjectRef>(entity);
                ecb.DestroyEntity(entity);
            }

            foreach (var entity in
                     SystemAPI.Query<RefRO<DeadTag>>()
                         .WithNone<GameObjectRef>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(entity.Item2);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
