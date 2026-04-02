using System.Collections;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SelStrom.Asteroids.Tests.PlayMode
{
    public class GameplayCycleTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameStarts_AndEntitiesExistInWorld()
        {
            // Wait for initialization
            yield return null;
            yield return null;
            yield return null;

            var entry = Object.FindFirstObjectByType<ApplicationEntry>();
            Assert.IsNotNull(entry, "ApplicationEntry should exist in scene");

            var world = World.DefaultGameObjectInjectionWorld;
            Assert.IsNotNull(world, "DefaultGameObjectInjectionWorld should exist");

            var shipQuery = world.EntityManager.CreateEntityQuery(typeof(ShipTag));
            Assert.GreaterOrEqual(shipQuery.CalculateEntityCount(), 1,
                "World should contain at least 1 Ship entity");

            var gameAreaQuery = world.EntityManager.CreateEntityQuery(typeof(GameAreaData));
            Assert.AreEqual(1, gameAreaQuery.CalculateEntityCount(),
                "GameArea singleton should exist");
        }

        [UnityTest]
        public IEnumerator GameLoop_EcsSystemsUpdateEveryFrame()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            var shipQuery = world.EntityManager.CreateEntityQuery(
                typeof(ShipTag),
                typeof(MoveData));

            if (shipQuery.CalculateEntityCount() > 0)
            {
                var entity = shipQuery.GetSingletonEntity();
                var moveData = world.EntityManager.GetComponentData<MoveData>(entity);
                Assert.IsFalse(float.IsNaN(moveData.Position.x),
                    "Ship position X should not be NaN");
                Assert.IsFalse(float.IsNaN(moveData.Position.y),
                    "Ship position Y should not be NaN");
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
        }
    }
}
