using System.Collections.Generic;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class CollisionBridge
    {
        private readonly Dictionary<GameObject, Entity> _goToEntity = new();
        private EntityManager _entityManager;
        private Entity _collisionBufferEntity;

        public void Initialize(EntityManager entityManager, Entity collisionBufferEntity)
        {
            _entityManager = entityManager;
            _collisionBufferEntity = collisionBufferEntity;
        }

        public void RegisterMapping(GameObject go, Entity entity)
        {
            _goToEntity[go] = entity;
        }

        public void UnregisterMapping(GameObject go)
        {
            _goToEntity.Remove(go);
        }

        public void ReportCollision(GameObject selfGo, GameObject otherGo)
        {
            if (!_goToEntity.TryGetValue(selfGo, out var selfEntity))
            {
                return;
            }

            if (!_goToEntity.TryGetValue(otherGo, out var otherEntity))
            {
                return;
            }

            var buffer = _entityManager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            buffer.Add(new CollisionEventData
            {
                EntityA = selfEntity,
                EntityB = otherEntity
            });
        }

        public void Clear()
        {
            _goToEntity.Clear();
        }
    }
}
