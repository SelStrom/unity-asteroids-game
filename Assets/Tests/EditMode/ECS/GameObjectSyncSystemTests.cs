using System.Collections.Generic;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class GameObjectSyncSystemTests : AsteroidsEcsTestFixture
    {
        private GameObjectSyncSystem _system;
        private List<GameObject> _testObjects;

        public override void SetUp()
        {
            base.SetUp();
            _system = World.CreateSystemManaged<GameObjectSyncSystem>();
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
        public void SyncsPositionAndRotation_ForEntityWithMoveAndRotateData()
        {
            var go = CreateTestGameObject();
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = new float2(5f, 3f)
            });
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = new float2(0f, 1f)
            });
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = go.transform,
                GameObject = go
            });

            _system.Update();

            Assert.AreEqual(5f, go.transform.position.x, 0.001f);
            Assert.AreEqual(3f, go.transform.position.y, 0.001f);
            Assert.AreEqual(90f, go.transform.eulerAngles.z, 0.1f);
        }

        [Test]
        public void SyncsPositionOnly_ForEntityWithoutRotateData()
        {
            var go = CreateTestGameObject();
            var initialRotation = go.transform.rotation;
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = new float2(2f, 7f)
            });
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = go.transform,
                GameObject = go
            });

            _system.Update();

            Assert.AreEqual(2f, go.transform.position.x, 0.001f);
            Assert.AreEqual(7f, go.transform.position.y, 0.001f);
            Assert.AreEqual(initialRotation, go.transform.rotation);
        }

        [Test]
        public void SyncsBothEntityTypes_InSingleUpdate()
        {
            var goWithRotate = CreateTestGameObject("withRotate");
            var goWithoutRotate = CreateTestGameObject("withoutRotate");

            var entityWithRotate = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entityWithRotate, new MoveData
            {
                Position = new float2(1f, 2f)
            });
            m_Manager.AddComponentData(entityWithRotate, new RotateData
            {
                Rotation = new float2(0f, 1f)
            });
            m_Manager.AddComponentObject(entityWithRotate, new GameObjectRef
            {
                Transform = goWithRotate.transform,
                GameObject = goWithRotate
            });

            var entityWithoutRotate = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entityWithoutRotate, new MoveData
            {
                Position = new float2(3f, 4f)
            });
            m_Manager.AddComponentObject(entityWithoutRotate, new GameObjectRef
            {
                Transform = goWithoutRotate.transform,
                GameObject = goWithoutRotate
            });

            _system.Update();

            Assert.AreEqual(1f, goWithRotate.transform.position.x, 0.001f);
            Assert.AreEqual(2f, goWithRotate.transform.position.y, 0.001f);
            Assert.AreEqual(3f, goWithoutRotate.transform.position.x, 0.001f);
            Assert.AreEqual(4f, goWithoutRotate.transform.position.y, 0.001f);
        }

        [Test]
        public void RotationZeroDegrees_ForRightDirection()
        {
            var go = CreateTestGameObject();
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = new float2(0f, 0f)
            });
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = new float2(1f, 0f)
            });
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = go.transform,
                GameObject = go
            });

            _system.Update();

            Assert.AreEqual(0f, go.transform.eulerAngles.z, 0.1f);
        }

        [Test]
        public void PreservesZPosition()
        {
            var go = CreateTestGameObject();
            go.transform.position = new Vector3(0f, 0f, -5f);
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = new float2(10f, 20f)
            });
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = go.transform,
                GameObject = go
            });

            _system.Update();

            Assert.AreEqual(10f, go.transform.position.x, 0.001f);
            Assert.AreEqual(20f, go.transform.position.y, 0.001f);
            Assert.AreEqual(-5f, go.transform.position.z, 0.001f);
        }
    }
}
