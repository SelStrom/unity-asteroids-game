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
            buffer.Add(new CollisionEventData
            {
                EntityA = entityA,
                EntityB = entityB
            });
        }

        private void RunSystem()
        {
            var system = CreateAndGetSystem<EcsCollisionHandlerSystem>();
            var systemHandle = World.GetExistingSystem<EcsCollisionHandlerSystem>();
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
        }

        [Test]
        public void RocketHitsAsteroid_BothGetDeadTag()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));
        }

        [Test]
        public void RocketHitsAsteroid_ScoreIncreased()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(100, scoreData.Value);
        }

        [Test]
        public void RocketHitsUfo_BothDead_AndScoreIncreased()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f));
            var ufo = CreateUfoEntity(
                new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 500);

            AddCollisionEvent(rocket, ufo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo));

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(500, scoreData.Value);
        }

        [Test]
        public void RocketHitsUfoBig_BothDead_AndScoreIncreased()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f));
            var ufoBig = CreateUfoBigEntity(
                new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 200);

            AddCollisionEvent(rocket, ufoBig);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig));

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(200, scoreData.Value);
        }

        [Test]
        public void RocketHitsAsteroid_ReversedOrder_BothDead_AndScoreIncreased()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(asteroid, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(100, scoreData.Value);
        }

        [Test]
        public void RocketHitsShip_NeitherGetsDeadTag()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f));
            var ship = CreateShipEntity(new float2(5f, 0f), 0f);

            AddCollisionEvent(rocket, ship);
            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(rocket),
                "Своя ракета не должна убивать корабль");
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(ship));
        }

        [Test]
        public void RocketHitsEnemyBullet_NoEffect()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f));
            var enemyBullet = CreateBulletEntity(
                new float2(5f, 0f), 20f, new float2(-1f, 0f), 2f, isPlayer: false);

            AddCollisionEvent(rocket, enemyBullet);
            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(enemyBullet));
        }
    }
}
