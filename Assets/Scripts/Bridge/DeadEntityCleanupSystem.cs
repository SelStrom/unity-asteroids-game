using System;
using System.Collections.Generic;
using SelStrom.Asteroids.ECS;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public struct DeadEntityInfo
    {
        public GameObject GameObject;
        public int Age;
        public float Speed;
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class DeadEntityCleanupSystem : SystemBase
    {
        private Action<DeadEntityInfo> _onDeadEntity;
        private readonly List<DeadEntityInfo> _deadEntities = new();

        public void SetOnDeadEntityCallback(Action<DeadEntityInfo> callback)
        {
            _onDeadEntity = callback;
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _deadEntities.Clear();

            // Собираем данные мёртвых entity ДО structural changes
            foreach (var (goRef, entity) in
                     SystemAPI.Query<GameObjectRef>()
                         .WithAll<DeadTag>()
                         .WithEntityAccess())
            {
                var info = new DeadEntityInfo
                {
                    GameObject = goRef.GameObject,
                    Age = -1,
                    Speed = 0f
                };

                if (EntityManager.HasComponent<AgeData>(entity))
                {
                    info.Age = EntityManager.GetComponentData<AgeData>(entity).Age;
                }

                if (EntityManager.HasComponent<MoveData>(entity))
                {
                    info.Speed = EntityManager.GetComponentData<MoveData>(entity).Speed;
                }

                _deadEntities.Add(info);
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

            // Вызываем callbacks после ECB playback,
            // т.к. callbacks могут делать structural changes (CreateAsteroid и т.д.)
            foreach (var info in _deadEntities)
            {
                _onDeadEntity?.Invoke(info);
            }
        }
    }
}
