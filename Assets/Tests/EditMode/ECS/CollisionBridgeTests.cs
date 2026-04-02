using System.Collections.Generic;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class CollisionBridgeTests : AsteroidsEcsTestFixture
    {
        private CollisionBridge _bridge;
        private Entity _collisionBufferEntity;
        private List<GameObject> _createdGameObjects;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _bridge = new CollisionBridge();
            _collisionBufferEntity = CreateCollisionEventSingleton();
            _bridge.Initialize(m_Manager, _collisionBufferEntity);
            _createdGameObjects = new List<GameObject>();
        }

        [TearDown]
        public override void TearDown()
        {
            foreach (var go in _createdGameObjects)
            {
                Object.DestroyImmediate(go);
            }

            _createdGameObjects.Clear();
            base.TearDown();
        }

        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _createdGameObjects.Add(go);
            return go;
        }

        [Test]
        public void ReportCollision_BothRegistered_AddsEventToBuffer()
        {
            var goA = CreateTestGameObject("EntityA");
            var goB = CreateTestGameObject("EntityB");
            var entityA = m_Manager.CreateEntity();
            var entityB = m_Manager.CreateEntity();

            _bridge.RegisterMapping(goA, entityA);
            _bridge.RegisterMapping(goB, entityB);

            _bridge.ReportCollision(goA, goB);

            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            Assert.AreEqual(1, buffer.Length, "Buffer should contain exactly 1 collision event");
            Assert.AreEqual(entityA, buffer[0].EntityA, "EntityA should match registered entity");
            Assert.AreEqual(entityB, buffer[0].EntityB, "EntityB should match registered entity");
        }

        [Test]
        public void ReportCollision_SelfNotRegistered_DoesNothing()
        {
            var goA = CreateTestGameObject("Unregistered");
            var goB = CreateTestGameObject("EntityB");
            var entityB = m_Manager.CreateEntity();

            _bridge.RegisterMapping(goB, entityB);

            _bridge.ReportCollision(goA, goB);

            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            Assert.AreEqual(0, buffer.Length, "Buffer should be empty when self is not registered");
        }

        [Test]
        public void ReportCollision_OtherNotRegistered_DoesNothing()
        {
            var goA = CreateTestGameObject("EntityA");
            var goB = CreateTestGameObject("Unregistered");
            var entityA = m_Manager.CreateEntity();

            _bridge.RegisterMapping(goA, entityA);

            _bridge.ReportCollision(goA, goB);

            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            Assert.AreEqual(0, buffer.Length, "Buffer should be empty when other is not registered");
        }

        [Test]
        public void UnregisterMapping_PreventsCollision()
        {
            var goA = CreateTestGameObject("EntityA");
            var goB = CreateTestGameObject("EntityB");
            var entityA = m_Manager.CreateEntity();
            var entityB = m_Manager.CreateEntity();

            _bridge.RegisterMapping(goA, entityA);
            _bridge.RegisterMapping(goB, entityB);
            _bridge.UnregisterMapping(goA);

            _bridge.ReportCollision(goA, goB);

            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            Assert.AreEqual(0, buffer.Length,
                "Buffer should be empty after unregistering self");
        }

        [Test]
        public void MultipleCollisions_AddMultipleEvents()
        {
            var goA = CreateTestGameObject("EntityA");
            var goB = CreateTestGameObject("EntityB");
            var goC = CreateTestGameObject("EntityC");
            var entityA = m_Manager.CreateEntity();
            var entityB = m_Manager.CreateEntity();
            var entityC = m_Manager.CreateEntity();

            _bridge.RegisterMapping(goA, entityA);
            _bridge.RegisterMapping(goB, entityB);
            _bridge.RegisterMapping(goC, entityC);

            _bridge.ReportCollision(goA, goB);
            _bridge.ReportCollision(goB, goC);
            _bridge.ReportCollision(goA, goC);

            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            Assert.AreEqual(3, buffer.Length,
                "Buffer should contain 3 collision events");
        }
    }
}
