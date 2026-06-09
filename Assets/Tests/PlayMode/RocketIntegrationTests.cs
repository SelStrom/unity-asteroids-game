using System.Collections;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SelStrom.Asteroids.Tests.PlayMode
{
    public class RocketIntegrationTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        private static IEnumerator StartGame()
        {
            // Шум LeaderboardService (повторный анонимный sign-in при перезагрузке
            // сцены между тестами) не относится к проверяемой логике ракет
            LogAssert.ignoreFailingMessages = true;
            var button = Object.FindFirstObjectByType<UnityEngine.UI.Button>();
            Assert.IsNotNull(button, "Start button should be active on title screen");
            button.onClick.Invoke();
            yield return null;
            yield return null;
        }

        private static Entity GetShip(EntityManager em)
        {
            var query = em.CreateEntityQuery(typeof(ShipTag));
            Assert.AreEqual(1, query.CalculateEntityCount(), "Ship should exist after game start");
            return query.GetSingletonEntity();
        }

        private static void RequestLaunch(EntityManager em, Entity ship)
        {
            // Повторяет Game.OnRocket — путь от нажатия R
            var launcher = em.GetComponentData<RocketLauncherData>(ship);
            var rotate = em.GetComponentData<RotateData>(ship);
            var move = em.GetComponentData<MoveData>(ship);
            launcher.Launching = true;
            launcher.LaunchDirection = rotate.Rotation;
            launcher.LaunchPosition = move.Position;
            em.SetComponentData(ship, launcher);
        }

        [UnityTest]
        public IEnumerator ShipHasRocketLauncher_WithConfiguredRocket()
        {
            yield return StartGame();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var ship = GetShip(em);

            Assert.IsTrue(em.HasComponent<RocketLauncherData>(ship));
            var launcher = em.GetComponentData<RocketLauncherData>(ship);
            Assert.Greater(launcher.MaxRockets, 0, "MaxRockets should come from config");
            Assert.AreEqual(launcher.MaxRockets, launcher.CurrentRockets);
        }

        [UnityTest]
        public IEnumerator LaunchRocket_CreatesRocketEntityWithView_AndStartsRespawnTimer()
        {
            yield return StartGame();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var ship = GetShip(em);
            RequestLaunch(em, ship);

            yield return null;
            yield return null;

            var launcher = em.GetComponentData<RocketLauncherData>(ship);
            Assert.AreEqual(launcher.MaxRockets - 1, launcher.CurrentRockets,
                "Launch should consume one rocket");
            Assert.IsFalse(launcher.Launching, "Launching flag should be reset");

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount(), "Rocket entity should exist");

            var rocket = rocketQuery.GetSingletonEntity();
            Assert.IsTrue(em.HasComponent<PlayerBulletTag>(rocket));
            Assert.IsTrue(em.HasComponent<GameObjectRef>(rocket), "Rocket should have a visual");
            var view = em.GetComponentObject<GameObjectRef>(rocket).GameObject;
            Assert.IsTrue(view.activeInHierarchy);
            Assert.IsNotNull(view.GetComponent<RocketVisual>());
            Assert.IsNotNull(view.GetComponentInChildren<ParticleSystem>(),
                "Rocket should have a particle trail");

            // Таймер респавна тикает после запуска
            var before = launcher.RespawnRemaining;
            yield return null;
            yield return null;
            var after = em.GetComponentData<RocketLauncherData>(ship).RespawnRemaining;
            Assert.Less(after, before, "Respawn timer should be counting down");
        }

        [UnityTest]
        public IEnumerator LaunchedRocket_AcquiresTarget_AndMoves()
        {
            yield return StartGame();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var ship = GetShip(em);
            RequestLaunch(em, ship);

            yield return null;
            yield return null;

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount());
            var rocket = rocketQuery.GetSingletonEntity();
            var startPosition = em.GetComponentData<MoveData>(rocket).Position;

            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.AreEqual(1, rocketQuery.CalculateEntityCount(), "Rocket should still fly");
            var homing = em.GetComponentData<RocketHomingData>(rocket);
            Assert.AreNotEqual(Entity.Null, homing.TargetEntity,
                "Rocket should acquire a target (asteroids exist at game start)");

            var position = em.GetComponentData<MoveData>(rocket).Position;
            Assert.Greater(Unity.Mathematics.math.distance(startPosition, position), 0f,
                "Rocket should move");
        }

        [UnityTest]
        public IEnumerator SecondLaunch_WithoutRockets_DoesNothing()
        {
            yield return StartGame();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var ship = GetShip(em);
            RequestLaunch(em, ship);
            yield return null;
            yield return null;

            // MaxRockets из конфига = 1: второй запуск не должен создать ракету
            RequestLaunch(em, ship);
            yield return null;
            yield return null;

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount(),
                "No second rocket without ammo");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return null;
        }
    }
}
