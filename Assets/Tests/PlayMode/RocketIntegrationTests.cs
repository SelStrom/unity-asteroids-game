using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SelStrom.Asteroids.Tests.PlayMode
{
    /// <summary>
    /// Интеграционные тесты конвейера ракеты: ECS-запуск -> RocketShootEvent ->
    /// ShootEventProcessorSystem -> EntitiesCatalog -> GameObject в сцене.
    /// </summary>
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
            var titleView = Object.FindFirstObjectByType<TitleScreenView>(
                FindObjectsInactive.Include);
            Assert.IsNotNull(titleView, "TitleScreenView должен быть в сцене");

            var buttonField = typeof(TitleScreenView).GetField(
                "_startButton", BindingFlags.NonPublic | BindingFlags.Instance);
            var button = (Button)buttonField.GetValue(titleView);
            button.onClick.Invoke();

            yield return null;
            yield return null;
        }

        private static bool TryGetShip(EntityManager em, out Entity ship)
        {
            var query = em.CreateEntityQuery(typeof(ShipTag));
            if (query.CalculateEntityCount() > 0)
            {
                ship = query.GetSingletonEntity();
                return true;
            }

            ship = Entity.Null;
            return false;
        }

        [UnityTest]
        public IEnumerator Ship_HasRocketLauncher_FromConfig()
        {
            yield return StartGame();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Assert.IsTrue(TryGetShip(em, out var ship), "Корабль должен существовать");

            Assert.IsTrue(em.HasComponent<RocketLauncherData>(ship),
                "На корабле должен быть RocketLauncherData");
            var launcher = em.GetComponentData<RocketLauncherData>(ship);
            Assert.Greater(launcher.MaxRockets, 0,
                "MaxRockets должен быть задан из конфига");
            Assert.AreEqual(launcher.MaxRockets, launcher.CurrentRockets,
                "На старте боезапас полон");
        }

        [UnityTest]
        public IEnumerator LaunchRocket_SpawnsEntityAndGameObject_AndStartsRespawn()
        {
            yield return StartGame();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Assert.IsTrue(TryGetShip(em, out var ship), "Корабль должен существовать");

            var launcher = em.GetComponentData<RocketLauncherData>(ship);
            var rotate = em.GetComponentData<RotateData>(ship);
            var move = em.GetComponentData<MoveData>(ship);
            launcher.Launching = true;
            launcher.Direction = rotate.Rotation;
            launcher.LaunchPosition = move.Position;
            em.SetComponentData(ship, launcher);

            // Кадры: запуск -> событие -> спавн GO
            yield return null;
            yield return null;
            yield return null;

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount(),
                "Должна появиться одна ракета-entity");

            var rocketEntity = rocketQuery.GetSingletonEntity();
            Assert.IsTrue(em.HasComponent<PlayerBulletTag>(rocketEntity),
                "Ракета помечена как пуля игрока");
            Assert.IsTrue(em.HasComponent<HomingData>(rocketEntity),
                "У ракеты есть наведение");

            // GameObjectPool присваивает инстансу имя префаба (без «(Clone)»)
            var rocketGo = GameObject.Find("rocket");
            Assert.IsNotNull(rocketGo, "GameObject ракеты должен появиться в сцене");

            launcher = em.GetComponentData<RocketLauncherData>(ship);
            Assert.AreEqual(launcher.MaxRockets - 1, launcher.CurrentRockets,
                "Боезапас уменьшился на 1");

            // Респавн-таймер тикает
            var before = launcher.RespawnRemaining;
            yield return null;
            yield return null;
            launcher = em.GetComponentData<RocketLauncherData>(ship);
            Assert.Less(launcher.RespawnRemaining, before,
                "После запуска включился счётчик респавна");
        }

        [UnityTest]
        public IEnumerator LaunchedRocket_MovesAndHoming_TracksNearestEnemy()
        {
            yield return StartGame();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Assert.IsTrue(TryGetShip(em, out var ship), "Корабль должен существовать");

            var launcher = em.GetComponentData<RocketLauncherData>(ship);
            var rotate = em.GetComponentData<RotateData>(ship);
            var move = em.GetComponentData<MoveData>(ship);
            launcher.Launching = true;
            launcher.Direction = rotate.Rotation;
            launcher.LaunchPosition = move.Position;
            em.SetComponentData(ship, launcher);

            yield return null;
            yield return null;
            yield return null;

            var rocketQuery = em.CreateEntityQuery(typeof(RocketTag));
            Assert.AreEqual(1, rocketQuery.CalculateEntityCount());
            var rocketEntity = rocketQuery.GetSingletonEntity();

            var startPos = em.GetComponentData<MoveData>(rocketEntity).Position;

            for (var i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.IsTrue(em.Exists(rocketEntity), "Ракета ещё жива");
            var homing = em.GetComponentData<HomingData>(rocketEntity);
            Assert.AreNotEqual(Entity.Null, homing.TargetEntity,
                "Ракета захватила цель (в сцене есть стартовые астероиды)");

            var currentPos = em.GetComponentData<MoveData>(rocketEntity).Position;
            Assert.AreNotEqual(startPos, currentPos, "Ракета движется");
        }
    }
}
