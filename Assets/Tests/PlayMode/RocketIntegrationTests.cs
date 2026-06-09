using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SelStrom.Asteroids.Tests.PlayMode
{
    /// <summary>
    /// Интеграционные тесты ракет в реальном игровом мире (default world,
    /// системы в player loop, зависимости выставлены Application.Start).
    /// </summary>
    public class RocketIntegrationTests
    {
        private readonly List<Entity> _createdEntities = new();

        private static EntityManager Manager =>
            World.DefaultGameObjectInjectionWorld.EntityManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _createdEntities.Clear();
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var em = Manager;
            foreach (var entity in _createdEntities)
            {
                if (em.Exists(entity))
                {
                    em.DestroyEntity(entity);
                }
            }

            // Подчистить заспавненные ракеты, чтобы тесты не влияли друг на друга
            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            em.DestroyEntity(rocketQuery);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RocketLaunchEvent_SpawnsRocketEntity_WithView()
        {
            var em = Manager;
            var bufferQuery = em.CreateEntityQuery(typeof(RocketLaunchEvent));
            Assert.AreEqual(1, bufferQuery.CalculateEntityCount(),
                "Синглтон RocketLaunchEvent должен создаваться при старте приложения");

            var bufferEntity = bufferQuery.GetSingletonEntity();
            em.GetBuffer<RocketLaunchEvent>(bufferEntity).Add(new RocketLaunchEvent
            {
                Position = new float2(0f, 0f),
                Direction = new float2(1f, 0f)
            });

            yield return null;
            yield return null;

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount(),
                "Событие запуска должно породить entity ракеты");

            var rocketEntity = rocketQuery.GetSingletonEntity();
            Assert.IsTrue(em.HasComponent<PlayerBulletTag>(rocketEntity),
                "Ракета должна считаться снарядом игрока");
            Assert.IsTrue(em.HasComponent<GameObjectRef>(rocketEntity),
                "Ракета должна получить визуал (GameObjectRef)");

            var goRef = em.GetComponentObject<GameObjectRef>(rocketEntity);
            Assert.IsNotNull(goRef.GameObject, "GameObject ракеты должен существовать");
            Assert.IsTrue(goRef.GameObject.activeInHierarchy,
                "Визуал ракеты должен быть активен");
            Assert.IsNotNull(goRef.GameObject.GetComponentInChildren<ParticleSystem>(),
                "У ракеты должен быть след из частиц");
        }

        [UnityTest]
        public IEnumerator Rocket_TurnsTowardNearestTarget_InGameWorld()
        {
            var em = Manager;

            // Цель справа-сверху, ракета летит вправо
            var asteroid = EntityFactory.CreateAsteroid(
                em, new float2(10f, 10f), 0f, new float2(1f, 0f), 1, 100);
            _createdEntities.Add(asteroid);

            var rocket = EntityFactory.CreateRocket(
                em, float2.zero, 0f, new float2(1f, 0f),
                lifeTime: 30f, turnSpeedDegPerSec: 150f);
            _createdEntities.Add(rocket);

            var initialDot = math.dot(
                math.normalize(new float2(1f, 0f)),
                math.normalize(new float2(10f, 10f)));

            for (var i = 0; i < 15; i++)
            {
                yield return null;
            }

            Assert.IsTrue(em.Exists(rocket), "Ракета ещё должна жить");
            var move = em.GetComponentData<MoveData>(rocket);
            var toTarget = math.normalize(
                new float2(10f, 10f) - move.Position);
            var dot = math.dot(move.Direction, toTarget);
            Assert.Greater(dot, initialDot,
                "Направление ракеты должно довернуться к цели за прошедшие кадры");

            Assert.AreEqual(asteroid,
                em.GetComponentData<RocketData>(rocket).TargetEntity,
                "Ракета должна выбрать ближайшую цель");
        }
    }
}
