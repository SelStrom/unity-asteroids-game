using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    // Ракета несёт PlayerBulletTag — попадание в любого врага (включая
    // «не выбранную» цель) обрабатывается штатным EcsCollisionHandlerSystem
    public class RocketCollisionTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;
        private Entity _eventEntity;
        private Entity _scoreEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsCollisionHandlerSystem>();
            _eventEntity = CreateCollisionEventSingleton();
            _scoreEntity = CreateScoreDataSingleton();
        }

        private void RunSystem()
        {
            World.PushTime(new TimeData(1f, 1f));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        private Entity CreateRocket()
        {
            return EntityFactory.CreateRocket(
                m_Manager, float2.zero, 8f, new float2(1f, 0f), 10f, 180f);
        }

        [Test]
        public void RocketHitsAsteroid_BothDead_ScoreAdded()
        {
            var rocket = CreateRocket();
            var asteroid = CreateAsteroidEntity(float2.zero, 1f, new float2(1f, 0f), 3, score: 100);

            var events = m_Manager.GetBuffer<CollisionEventData>(_eventEntity);
            events.Add(new CollisionEventData { EntityA = rocket, EntityB = asteroid });

            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));
            Assert.AreEqual(100, m_Manager.GetComponentData<ScoreData>(_scoreEntity).Value);
        }

        [Test]
        public void RocketHitsUfo_NotItsTarget_StillCounts()
        {
            var intendedTarget = CreateAsteroidEntity(new float2(20f, 0f), 1f, new float2(1f, 0f), 3);
            var rocket = CreateRocket();
            m_Manager.SetComponentData(rocket, new RocketHomingData
            {
                TargetEntity = intendedTarget,
                TurnRateDegPerSec = 180f
            });
            var ufo = CreateUfoEntity(float2.zero, 1f, new float2(1f, 0f), score: 500);

            var events = m_Manager.GetBuffer<CollisionEventData>(_eventEntity);
            events.Add(new CollisionEventData { EntityA = ufo, EntityB = rocket });

            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo));
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(intendedTarget));
            Assert.AreEqual(500, m_Manager.GetComponentData<ScoreData>(_scoreEntity).Value);
        }
    }
}
