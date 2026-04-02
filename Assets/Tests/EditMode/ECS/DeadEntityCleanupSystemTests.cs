using System.Collections.Generic;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class DeadEntityCleanupSystemTests : AsteroidsEcsTestFixture
    {
        private DeadEntityCleanupSystem _system;
        private List<GameObject> _callbackResults;
        private List<GameObject> _createdGameObjects;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.AddSystemManaged(new DeadEntityCleanupSystem());
            _callbackResults = new List<GameObject>();
            _createdGameObjects = new List<GameObject>();
            _system.SetOnDeadEntityCallback(go => _callbackResults.Add(go));
        }

        [TearDown]
        public override void TearDown()
        {
            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
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
        public void CleansUp_EntityWithDeadTagAndGameObjectRef()
        {
            var go = CreateTestGameObject("DeadEntity");
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new DeadTag());
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                GameObject = go,
                Transform = go.transform
            });

            _system.Update();

            Assert.AreEqual(1, _callbackResults.Count,
                "Callback should be called once");
            Assert.AreEqual(go, _callbackResults[0],
                "Callback should receive the correct GameObject");
            Assert.IsFalse(m_Manager.Exists(entity),
                "Entity should be destroyed");
        }

        [Test]
        public void Destroys_EntityWithDeadTagWithoutGameObjectRef()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new DeadTag());

            _system.Update();

            Assert.AreEqual(0, _callbackResults.Count,
                "Callback should not be called without GameObjectRef");
            Assert.IsFalse(m_Manager.Exists(entity),
                "Entity should be destroyed");
        }

        [Test]
        public void Ignores_EntityWithoutDeadTag()
        {
            var go = CreateTestGameObject("AliveEntity");
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                GameObject = go,
                Transform = go.transform
            });

            _system.Update();

            Assert.AreEqual(0, _callbackResults.Count,
                "Callback should not be called without DeadTag");
            Assert.IsTrue(m_Manager.Exists(entity),
                "Entity without DeadTag should not be destroyed");
        }

        [Test]
        public void HandlesMultipleDeadEntities()
        {
            var go1 = CreateTestGameObject("Dead1");
            var go2 = CreateTestGameObject("Dead2");
            var go3 = CreateTestGameObject("Dead3");

            var entity1 = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity1, new DeadTag());
            m_Manager.AddComponentObject(entity1, new GameObjectRef
            {
                GameObject = go1,
                Transform = go1.transform
            });

            var entity2 = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity2, new DeadTag());
            m_Manager.AddComponentObject(entity2, new GameObjectRef
            {
                GameObject = go2,
                Transform = go2.transform
            });

            var entity3 = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity3, new DeadTag());
            m_Manager.AddComponentObject(entity3, new GameObjectRef
            {
                GameObject = go3,
                Transform = go3.transform
            });

            _system.Update();

            Assert.AreEqual(3, _callbackResults.Count,
                "All 3 dead entities should trigger callback");
            Assert.IsFalse(m_Manager.Exists(entity1),
                "Entity1 should be destroyed");
            Assert.IsFalse(m_Manager.Exists(entity2),
                "Entity2 should be destroyed");
            Assert.IsFalse(m_Manager.Exists(entity3),
                "Entity3 should be destroyed");
        }
    }
}
