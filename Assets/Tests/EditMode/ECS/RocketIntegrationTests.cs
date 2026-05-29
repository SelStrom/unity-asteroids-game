using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    /// <summary>
    /// Интеграционные тесты ракеты: несколько ECS-систем работают вместе по конвейеру
    /// (запуск → событие; наведение + движение; время жизни → смерть; коллизия → очки).
    /// </summary>
    public class RocketIntegrationTests : AsteroidsEcsTestFixture
    {
        [Test]
        public void Pipeline_LauncherEmitsEvent_OnLaunchRequest()
        {
            var launcherSystem = World.CreateSystem<EcsRocketLauncherSystem>();
            var eventSingleton = CreateRocketLaunchEventSingleton();

            var ship = CreateShipEntity(float2.zero, 0f);
            m_Manager.AddComponentData(ship, new RocketLauncherData
            {
                MaxRockets = 1,
                ReloadDurationSec = 10f,
                CurrentRockets = 1,
                ReloadRemaining = 10f,
                Launching = true,
                LaunchPosition = new float2(2f, 1f),
                LaunchDirection = new float2(1f, 0f)
            });

            World.PushTime(new TimeData(0.1f, 0.1f));
            launcherSystem.Update(World.Unmanaged);
            World.PopTime();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(eventSingleton);
            Assert.AreEqual(1, buffer.Length, "запуск должен породить событие RocketLaunchEvent");
            Assert.AreEqual(new float2(2f, 1f), buffer[0].Position);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(ship);
            Assert.AreEqual(0, launcher.CurrentRockets, "ракета должна списаться");
        }

        [Test]
        public void Pipeline_HomingAndMove_RocketConvergesOnTarget()
        {
            CreateGameAreaSingleton(new float2(1000f, 1000f));

            var moveSystem = World.CreateSystem<EcsMoveSystem>();
            var homingSystem = World.CreateSystem<EcsRocketHomingSystem>();

            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 8f, direction: new float2(1f, 0f),
                turnRateDegPerSec: 180f);
            var enemyPos = new float2(6f, 2f);
            CreateAsteroidEntity(enemyPos, 0f, float2.zero, 1);

            var initialDistance = math.distance(float2.zero, enemyPos);
            var minDistance = initialDistance;

            for (var i = 0; i < 300; i++)
            {
                World.PushTime(new TimeData(0.02f, 0.02f));
                moveSystem.Update(World.Unmanaged);   // движение по текущему направлению
                homingSystem.Update(World.Unmanaged); // доворот к цели
                World.PopTime();

                var pos = m_Manager.GetComponentData<MoveData>(rocket).Position;
                minDistance = math.min(minDistance, math.distance(pos, enemyPos));
            }

            Assert.Less(minDistance, initialDistance,
                "ракета должна приблизиться к цели");
            Assert.Less(minDistance, 2.0f,
                "ракета должна выйти к цели на дистанцию поражения (наведение работает)");
        }

        [Test]
        public void Pipeline_LifeTime_RocketDiesAfterTimeout()
        {
            var lifeTimeSystem = World.CreateSystem<EcsLifeTimeSystem>();
            var deadSystem = World.CreateSystem<EcsDeadByLifeTimeSystem>();

            var rocket = EntityFactory.CreateRocket(
                m_Manager, float2.zero, 8f, new float2(1f, 0f),
                turnRateDegPerSec: 180f, lifeTime: 0.05f);

            World.PushTime(new TimeData(0.1f, 0.1f));
            lifeTimeSystem.Update(World.Unmanaged);
            deadSystem.Update(World.Unmanaged);
            World.PopTime();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "ракета должна получить DeadTag по истечении времени жизни");
        }

        [Test]
        public void Pipeline_Collision_RocketDestroysEnemyAndScores()
        {
            var scoreEntity = CreateScoreDataSingleton(0);
            var collisionBuffer = CreateCollisionEventSingleton();

            var rocket = CreateRocketEntity(float2.zero, 12f, new float2(1f, 0f), 120f);
            var asteroid = CreateAsteroidEntity(new float2(1f, 0f), 0f, float2.zero, 1, score: 100);

            m_Manager.GetBuffer<CollisionEventData>(collisionBuffer).Add(new CollisionEventData
            {
                EntityA = rocket,
                EntityB = asteroid
            });

            var system = CreateAndGetSystem<EcsCollisionHandlerSystem>();
            var handle = World.GetExistingSystem<EcsCollisionHandlerSystem>();
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(handle));

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));
            Assert.AreEqual(100, m_Manager.GetComponentData<ScoreData>(scoreEntity).Value);
        }
    }
}
