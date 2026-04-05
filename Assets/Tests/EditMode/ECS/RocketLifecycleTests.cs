using System.Collections.Generic;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketLifecycleTests : AsteroidsEcsTestFixture
    {
        private GameObjectSyncSystem _syncSystem;
        private DeadEntityCleanupSystem _cleanupSystem;
        private List<DeadEntityInfo> _callbackResults;
        private List<GameObject> _testObjects;

        public override void SetUp()
        {
            base.SetUp();
            _syncSystem = World.CreateSystemManaged<GameObjectSyncSystem>();
            _cleanupSystem = World.AddSystemManaged(new DeadEntityCleanupSystem());
            _callbackResults = new List<DeadEntityInfo>();
            _cleanupSystem.SetOnDeadEntityCallback(info => _callbackResults.Add(info));
            _testObjects = new List<GameObject>();
        }

        public override void TearDown()
        {
            foreach (var go in _testObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _testObjects.Clear();
            base.TearDown();
        }

        private GameObject CreateTestGameObject(string name = "test")
        {
            var go = new GameObject(name);
            _testObjects.Add(go);
            return go;
        }

        [Test]
        public void RocketLifecycle_SpawnEntityWithComponents()
        {
            var entity = CreateRocketEntity(float2.zero, 8f, new float2(1f, 0f), 5f, 180f);

            Assert.IsTrue(m_Manager.Exists(entity));
            Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RocketTargetData>(entity));
            Assert.IsFalse(m_Manager.HasComponent<RotateData>(entity));
        }

        [Test]
        public void RocketLifecycle_GameObjectRefSync()
        {
            var entity = CreateRocketEntity(new float2(5f, 3f), 8f, new float2(0f, 1f), 5f);
            var go = CreateTestGameObject("Rocket");
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = go.transform,
                GameObject = go
            });

            _syncSystem.Update();

            Assert.AreEqual(5f, go.transform.position.x, 0.001f);
            Assert.AreEqual(3f, go.transform.position.y, 0.001f);
            Assert.AreEqual(90f, go.transform.eulerAngles.z, 0.1f);
        }

        [Test]
        public void RocketLifecycle_DeadTagTriggersCleanup()
        {
            var entity = CreateRocketEntity(float2.zero, 8f, new float2(1f, 0f), 5f);
            var go = CreateTestGameObject("Rocket");
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = go.transform,
                GameObject = go
            });

            m_Manager.AddComponentData(entity, new DeadTag());
            _cleanupSystem.Update();

            Assert.IsFalse(m_Manager.Exists(entity));
            Assert.AreEqual(1, _callbackResults.Count);
            Assert.AreEqual(go, _callbackResults[0].GameObject);
        }

        [Test]
        public void RocketLifecycle_FullCycle_SpawnSyncDead()
        {
            var entity = CreateRocketEntity(new float2(1f, 2f), 8f, new float2(1f, 0f), 5f);
            var go = CreateTestGameObject("Rocket");
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = go.transform,
                GameObject = go
            });

            // Шаг 1: начальная синхронизация
            _syncSystem.Update();
            Assert.AreEqual(1f, go.transform.position.x, 0.001f);
            Assert.AreEqual(2f, go.transform.position.y, 0.001f);
            Assert.AreEqual(0f, go.transform.eulerAngles.z, 0.1f);

            // Шаг 2: обновление позиции и направления
            m_Manager.SetComponentData(entity, new MoveData
            {
                Position = new float2(3f, 4f),
                Speed = 8f,
                Direction = new float2(0f, 1f)
            });
            _syncSystem.Update();
            Assert.AreEqual(3f, go.transform.position.x, 0.001f);
            Assert.AreEqual(4f, go.transform.position.y, 0.001f);
            Assert.AreEqual(90f, go.transform.eulerAngles.z, 0.1f);

            // Шаг 3: уничтожение
            m_Manager.AddComponentData(entity, new DeadTag());
            _cleanupSystem.Update();
            Assert.IsFalse(m_Manager.Exists(entity));
            Assert.AreEqual(1, _callbackResults.Count);
        }

        [Test]
        public void RocketLifecycle_NoRotateData_NeverAdded()
        {
            var entity = CreateRocketEntity(float2.zero, 8f, new float2(1f, 0f), 5f);

            Assert.IsFalse(m_Manager.HasComponent<RotateData>(entity));
        }
    }
}
