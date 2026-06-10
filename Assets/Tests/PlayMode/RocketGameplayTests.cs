using System.Collections;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SelStrom.Asteroids.Tests.PlayMode
{
    public class RocketGameplayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Ошибки UGS-сервисов (лидерборд/аутентификация) нерелевантны для ракетных тестов
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        private static IEnumerator StartGame()
        {
            var titleScreen = Object.FindFirstObjectByType<TitleScreenView>(FindObjectsInactive.Include);
            Assert.IsNotNull(titleScreen, "TitleScreenView should exist in scene");

            var startButton = titleScreen.GetComponentInChildren<Button>(true);
            Assert.IsNotNull(startButton, "Start button should exist on title screen");
            startButton.onClick.Invoke();

            yield return null;
            yield return null;
        }

        private static Entity GetShipEntity(EntityManager em)
        {
            var query = em.CreateEntityQuery(typeof(ShipTag), typeof(RocketAmmoData));
            Assert.AreEqual(1, query.CalculateEntityCount(),
                "Ship with RocketAmmoData should exist after game start");
            return query.GetSingletonEntity();
        }

        private static void LaunchRocket(EntityManager em, Entity ship)
        {
            var ammo = em.GetComponentData<RocketAmmoData>(ship);
            var rotate = em.GetComponentData<RotateData>(ship);
            var move = em.GetComponentData<MoveData>(ship);

            ammo.Launching = true;
            ammo.Direction = rotate.Rotation;
            ammo.LaunchPosition = move.Position;
            em.SetComponentData(ship, ammo);
        }

        [UnityTest]
        public IEnumerator LaunchRocket_EntityCreatedAmmoSpentAndHudUpdated()
        {
            yield return StartGame();

            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;
            var ship = GetShipEntity(em);

            var ammo = em.GetComponentData<RocketAmmoData>(ship);
            Assert.AreEqual(1, ammo.MaxRockets, "Config should give the player one rocket");
            Assert.AreEqual(1, ammo.CurrentRockets, "Player should start with a full rocket");

            LaunchRocket(em, ship);

            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount(),
                "Rocket entity should be created after launch");

            var rocket = rocketQuery.GetSingletonEntity();
            Assert.IsTrue(em.HasComponent<PlayerBulletTag>(rocket),
                "Rocket must be a player projectile");
            Assert.IsTrue(em.HasComponent<RocketData>(rocket));
            Assert.IsTrue(em.HasComponent<LifeTimeData>(rocket));
            Assert.IsTrue(em.HasComponent<GameObjectRef>(rocket),
                "Rocket must have a visual GameObject");

            var goRef = em.GetComponentObject<GameObjectRef>(rocket);
            Assert.IsNotNull(goRef.GameObject, "Rocket GameObject should exist");
            Assert.IsTrue(goRef.GameObject.activeInHierarchy, "Rocket GameObject should be active");

            ammo = em.GetComponentData<RocketAmmoData>(GetShipEntity(em));
            Assert.AreEqual(0, ammo.CurrentRockets, "Rocket ammo should be spent");

            var rocketCountText = GameObject.Find("rocket_count");
            Assert.IsNotNull(rocketCountText, "rocket_count HUD text should exist in scene");
            Assert.AreEqual("Rockets: 0", rocketCountText.GetComponent<TMP_Text>().text,
                "HUD should show spent rocket ammo");

            var rocketRespawnText = GameObject.Find("rocket_respawn_time");
            Assert.IsNotNull(rocketRespawnText,
                "rocket_respawn_time HUD text should be visible while ammo below max");
        }

        [UnityTest]
        public IEnumerator RocketRespawn_RestoresAmmoAndHud()
        {
            yield return StartGame();

            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;
            var ship = GetShipEntity(em);

            LaunchRocket(em, ship);

            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            ship = GetShipEntity(em);
            var ammo = em.GetComponentData<RocketAmmoData>(ship);
            Assert.AreEqual(0, ammo.CurrentRockets, "Rocket ammo should be spent before respawn");

            // Ускоряем респавн, чтобы не ждать полные RespawnDurationSec.
            // WaitForSeconds вместо кадров: в расфокусированном редакторе deltaTime крошечный.
            ammo.RespawnRemaining = 0.001f;
            em.SetComponentData(ship, ammo);

            yield return new WaitForSeconds(0.25f);
            yield return null;

            ammo = em.GetComponentData<RocketAmmoData>(GetShipEntity(em));
            Assert.AreEqual(1, ammo.CurrentRockets, "Rocket should respawn after timer expires");
            Assert.AreEqual(ammo.RespawnDurationSec, ammo.RespawnRemaining, 0.001f,
                "Respawn timer should be reset after restoring a rocket");

            var rocketCountText = GameObject.Find("rocket_count");
            Assert.IsNotNull(rocketCountText, "rocket_count HUD text should exist in scene");
            Assert.AreEqual("Rockets: 1", rocketCountText.GetComponent<TMP_Text>().text,
                "HUD should show restored rocket ammo");
        }

        [UnityTest]
        public IEnumerator RocketLifeTime_ExpiresAndVisualReleased()
        {
            yield return StartGame();

            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;
            var ship = GetShipEntity(em);

            LaunchRocket(em, ship);

            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount());

            // Ускоряем истечение времени жизни.
            // WaitForSeconds вместо кадров: в расфокусированном редакторе deltaTime крошечный.
            var rocket = rocketQuery.GetSingletonEntity();
            em.SetComponentData(rocket, new LifeTimeData { TimeRemaining = 0.001f });

            yield return new WaitForSeconds(0.25f);
            yield return null;

            Assert.AreEqual(0, em.CreateEntityQuery(typeof(RocketTag)).CalculateEntityCount(),
                "Rocket entity should be destroyed after lifetime expires");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            yield return null;
        }
    }
}
