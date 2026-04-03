using System;
using System.Collections.Generic;
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
        private readonly List<GameObject> _deadGameObjects = new();

        public void SetOnDeadEntityCallback(Action<GameObject> callback)
        {
            _onDeadEntity = callback;
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _deadGameObjects.Clear();

            // Собираем мёртвые GameObject'ы перед structural changes
            foreach (var (goRef, entity) in
                     SystemAPI.Query<GameObjectRef>()
                         .WithAll<DeadTag>()
                         .WithEntityAccess())
            {
                _deadGameObjects.Add(goRef.GameObject);
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

            // Вызываем callbacks после завершения итерации и ECB playback,
            // т.к. callbacks могут делать structural changes (CreateAsteroid и т.д.)
            foreach (var go in _deadGameObjects)
            {
                _onDeadEntity?.Invoke(go);
            }
        }
    }
}
