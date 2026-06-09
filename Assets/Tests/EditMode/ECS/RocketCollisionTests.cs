using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    // Ракета несёт PlayerBulletTag и обрабатывается EcsCollisionHandlerSystem
    // как пуля игрока: попадание в ЛЮБОГО врага (не только выбранную цель)
    // уничтожает обоих и начисляет очки.
    public class RocketCollisionTests : AsteroidsEcsTestFixture
    {
        private Entity _collisionSingleton;
        private Entity _scoreSingleton;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsCollisionHandlerSystem>();
            _collisionSingleton = CreateCollisionEventSingleton();
            _scoreSingleton = CreateScoreDataSingleton();
        }

        private void RunSystem()
        {
            _systemHandle.Update(World.Unmanaged);
        }

        private void ReportCollision(Entity a, Entity b)
        {
            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionSingleton);
            buffer.Add(new CollisionEventData { EntityA = a, EntityB = b });
        }

        [Test]
        public void RocketHitsAsteroid_BothDead_ScoreAdded()
        {
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(new float2(1f, 0f), 0f, new float2(1f, 0f), 1, 100);

            ReportCollision(rocket, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));
            Assert.AreEqual(100, m_Manager.GetComponentData<ScoreData>(_scoreSingleton).Value);
        }

        [Test]
        public void RocketHitsUfo_BothDead_ScoreAdded()
        {
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));
            var ufo = CreateUfoEntity(new float2(1f, 0f), 0f, new float2(1f, 0f), 500);

            ReportCollision(ufo, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo));
            Assert.AreEqual(500, m_Manager.GetComponentData<ScoreData>(_scoreSingleton).Value);
        }

        [Test]
        public void RocketHitsNonTargetedEnemy_StillCounts()
        {
            // Ракета выбрала целью дальний астероид, но столкнулась с другим врагом —
            // попадание тоже засчитывается
            var targetAsteroid = CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 1, 100);
            var incidentalUfo = CreateUfoBigEntity(new float2(1f, 0f), 0f, new float2(1f, 0f), 200);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));
            m_Manager.SetComponentData(rocket, new RocketData
            {
                TargetEntity = targetAsteroid,
                TurnSpeedDegPerSec = 180f
            });

            ReportCollision(rocket, incidentalUfo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(incidentalUfo));
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(targetAsteroid));
            Assert.AreEqual(200, m_Manager.GetComponentData<ScoreData>(_scoreSingleton).Value);
        }
    }
}
