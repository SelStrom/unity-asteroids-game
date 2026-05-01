using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class CollisionHandlerRocketTests : AsteroidsEcsTestFixture
    {
        private Entity _scoreEntity;
        private Entity _collisionBufferEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _scoreEntity = CreateScoreDataSingleton(0);
            _collisionBufferEntity = CreateCollisionEventSingleton();
        }

        private Entity CreateRocket(float2 position)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketTag());
            m_Manager.AddComponentData(entity, new PlayerRocketTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Direction = new float2(1f, 0f),
                Speed = 12f
            });
            m_Manager.AddComponentData(entity, new RocketHomingData());
            m_Manager.AddComponentData(entity, new LifeTimeData { TimeRemaining = 6f });
            return entity;
        }

        private void AddCollisionEvent(Entity entityA, Entity entityB)
        {
            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            buffer.Add(new CollisionEventData { EntityA = entityA, EntityB = entityB });
        }

        private void RunSystem()
        {
            var system = CreateAndGetSystem<EcsCollisionHandlerSystem>();
            var systemHandle = World.GetExistingSystem<EcsCollisionHandlerSystem>();
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
        }

        [Test]
        public void PlayerRocketHitsAsteroid_BothGetDeadTag()
        {
            var rocket = CreateRocket(float2.zero);
            var asteroid = CreateAsteroidEntity(new float2(5f, 0f), 0f, default, 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));
        }

        [Test]
        public void PlayerRocketHitsAsteroid_ScoreIncreasedByAsteroidScoreValue()
        {
            var rocket = CreateRocket(float2.zero);
            var asteroid = CreateAsteroidEntity(new float2(5f, 0f), 0f, default, 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(100, scoreData.Value);
        }

        [Test]
        public void PlayerRocketHitsUfoBig_BothDeadAndScoreIncreased()
        {
            var rocket = CreateRocket(float2.zero);
            var ufoBig = CreateUfoBigEntity(new float2(5f, 0f), 0f, default, score: 200);

            AddCollisionEvent(rocket, ufoBig);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig));
            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(200, scoreData.Value);
        }

        [Test]
        public void PlayerRocketHitsUfo_BothDeadAndScoreIncreased()
        {
            var rocket = CreateRocket(float2.zero);
            var ufo = CreateUfoEntity(new float2(5f, 0f), 0f, default, score: 500);

            AddCollisionEvent(rocket, ufo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo));
            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(500, scoreData.Value);
        }

        [Test]
        public void PlayerRocketHitsAsteroid_OrderReversed_StillDeadAndScoreCorrect()
        {
            var rocket = CreateRocket(float2.zero);
            var asteroid = CreateAsteroidEntity(new float2(5f, 0f), 0f, default, 3, score: 100);

            AddCollisionEvent(asteroid, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));
            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(100, scoreData.Value);
        }
    }
}
