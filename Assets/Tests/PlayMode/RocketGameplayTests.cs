using System.Collections;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SelStrom.Asteroids.Tests.PlayMode
{
    public class RocketGameplayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RocketShootEvent_DecrementsCurrentShoots()
        {
            yield return null;
            yield return null;

            var world = World.DefaultGameObjectInjectionWorld;
            Assert.IsNotNull(world);
            var em = world.EntityManager;

            var shipEntity = EntityFactory.CreateShip(em,
                position: float2.zero,
                moveSpeed: 0f,
                thrustAcceleration: 6f,
                thrustMaxSpeed: 6f,
                gunMaxShoots: 3,
                gunReloadSec: 1f,
                laserMaxShoots: 2,
                laserReloadSec: 5f,
                rocketMaxShoots: 1,
                rocketReloadSec: 5f);

            if (em.CreateEntityQuery(typeof(RocketShootEvent)).CalculateEntityCount() == 0)
            {
                var bufferEntity = em.CreateEntity();
                em.AddBuffer<RocketShootEvent>(bufferEntity);
            }

            var rocket = em.GetComponentData<RocketData>(shipEntity);
            rocket.Shooting = true;
            rocket.Direction = new float2(1f, 0f);
            rocket.ShootPosition = float2.zero;
            em.SetComponentData(shipEntity, rocket);

            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            var rocketAfter = em.GetComponentData<RocketData>(shipEntity);
            Assert.AreEqual(0, rocketAfter.CurrentShoots, "ракета должна быть выпущена (CurrentShoots=0)");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
        }
    }
}
