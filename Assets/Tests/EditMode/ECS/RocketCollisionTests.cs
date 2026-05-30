using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketCollisionTests : AsteroidsEcsTestFixture
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

        private void AddCollisionEvent(Entity entityA, Entity entityB)
        {
            var buffer = m_Manager.GetBuffer<CollisionEventData>(_collisionBufferEntity);
            buffer.Add(new CollisionEventData { EntityA = entityA, EntityB = entityB });
        }

        private void RunSystem()
        {
            var system = CreateAndGetSystem<EcsCollisionHandlerSystem>();
            var handle = World.GetExistingSystem<EcsCollisionHandlerSystem>();
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(handle));
        }

        [Test]
        public void RocketHitsAsteroid_BothDead_ScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(new float2(2f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket), "Rocket should die");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid), "Asteroid should die");
            Assert.AreEqual(100, m_Manager.GetComponentData<ScoreData>(_scoreEntity).Value);
        }

        [Test]
        public void RocketHitsUfo_BothDead_ScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ufo = CreateUfoEntity(new float2(2f, 0f), 2f, new float2(-1f, 0f), score: 500);

            AddCollisionEvent(rocket, ufo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo));
            Assert.AreEqual(500, m_Manager.GetComponentData<ScoreData>(_scoreEntity).Value);
        }

        [Test]
        public void RocketHitsUfoBig_ReversedOrder_BothDead()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ufoBig = CreateUfoBigEntity(new float2(2f, 0f), 2f, new float2(-1f, 0f), score: 200);

            AddCollisionEvent(ufoBig, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig));
            Assert.AreEqual(200, m_Manager.GetComponentData<ScoreData>(_scoreEntity).Value);
        }
    }
}
