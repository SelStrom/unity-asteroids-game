using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class CollisionHandlerTests : AsteroidsEcsTestFixture
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
        public void PlayerBulletHitsAsteroid_BothGetDeadTag()
        {
            var bullet = CreateBulletEntity(
                float2.zero, 20f, new float2(1f, 0f), 2f, isPlayer: true);
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(bullet, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(bullet),
                "Player bullet should get DeadTag");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag");
        }

        [Test]
        public void PlayerBulletHitsAsteroid_ScoreIncreased()
        {
            var bullet = CreateBulletEntity(
                float2.zero, 20f, new float2(1f, 0f), 2f, isPlayer: true);
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(bullet, asteroid);
            RunSystem();

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(100, scoreData.Value,
                "Score should increase by asteroid ScoreValue");
        }

        [Test]
        public void PlayerBulletHitsUfoBig_BothDeadAndScoreIncreased()
        {
            var bullet = CreateBulletEntity(
                float2.zero, 20f, new float2(1f, 0f), 2f, isPlayer: true);
            var ufoBig = CreateUfoBigEntity(
                new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 200);

            AddCollisionEvent(bullet, ufoBig);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(bullet),
                "Player bullet should get DeadTag");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig),
                "UfoBig should get DeadTag");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(200, scoreData.Value,
                "Score should increase by UfoBig ScoreValue");
        }

        [Test]
        public void EnemyBulletHitsShip_BothGetDeadTag()
        {
            var bullet = CreateBulletEntity(
                float2.zero, 20f, new float2(1f, 0f), 2f, isPlayer: false);
            var ship = CreateShipEntity(new float2(5f, 0f), 0f);

            AddCollisionEvent(bullet, ship);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(bullet),
                "Enemy bullet should get DeadTag");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ship),
                "Ship should get DeadTag");
        }

        [Test]
        public void ShipHitsAsteroid_ShipGetDeadTag()
        {
            var ship = CreateShipEntity(new float2(0f, 0f), 5f);
            var asteroid = CreateAsteroidEntity(
                new float2(1f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(ship, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ship),
                "Ship should get DeadTag on collision with asteroid");
        }

        [Test]
        public void AsteroidHitsUfo_BothGetDeadTag()
        {
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);
            var ufo = CreateUfoEntity(
                new float2(0f, 0f), 2f, new float2(1f, 0f), score: 500);

            AddCollisionEvent(asteroid, ufo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag on collision with Ufo");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo),
                "Ufo should get DeadTag on collision with Asteroid");
        }

        [Test]
        public void AsteroidHitsUfoBig_BothGetDeadTag()
        {
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);
            var ufoBig = CreateUfoBigEntity(
                new float2(0f, 0f), 2f, new float2(1f, 0f), score: 200);

            AddCollisionEvent(asteroid, ufoBig);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag on collision with UfoBig");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig),
                "UfoBig should get DeadTag on collision with Asteroid");
        }

        [Test]
        public void AsteroidHitsUfo_ScoreNotChanged()
        {
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);
            var ufo = CreateUfoEntity(
                new float2(0f, 0f), 2f, new float2(1f, 0f), score: 500);

            AddCollisionEvent(asteroid, ufo);
            RunSystem();

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(0, scoreData.Value,
                "Score should remain 0 when Asteroid collides with Ufo (no player involvement)");
        }

        [Test]
        public void AsteroidHitsUfo_ReversedOrder_BothGetDeadTag()
        {
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);
            var ufo = CreateUfoEntity(
                new float2(0f, 0f), 2f, new float2(1f, 0f), score: 500);

            AddCollisionEvent(ufo, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag when Ufo is entityA");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo),
                "Ufo should get DeadTag when Ufo is entityA");
        }

        [Test]
        public void NoCollisionEvents_NothingHappens()
        {
            var ship = CreateShipEntity(new float2(0f, 0f), 5f);
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(ship),
                "Ship should not have DeadTag without collision");
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should not have DeadTag without collision");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(0, scoreData.Value,
                "Score should remain 0 without collisions");
        }

        [Test]
        public void RocketHitsAsteroid_BothGetDeadTag()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f), 5f);
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag on collision with asteroid");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag on collision with rocket");
        }

        [Test]
        public void RocketHitsAsteroid_ScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f), 5f);
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(100, scoreData.Value,
                "Score should increase by asteroid ScoreValue when hit by rocket");
        }

        [Test]
        public void RocketHitsUfo_BothDeadAndScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f), 5f);
            var ufo = CreateUfoEntity(
                new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 500);

            AddCollisionEvent(rocket, ufo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag on collision with Ufo");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo),
                "Ufo should get DeadTag on collision with rocket");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(500, scoreData.Value,
                "Score should increase by Ufo ScoreValue when hit by rocket");
        }

        [Test]
        public void RocketHitsUfoBig_BothDeadAndScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f), 5f);
            var ufoBig = CreateUfoBigEntity(
                new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 200);

            AddCollisionEvent(rocket, ufoBig);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag on collision with UfoBig");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig),
                "UfoBig should get DeadTag on collision with rocket");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(200, scoreData.Value,
                "Score should increase by UfoBig ScoreValue when hit by rocket");
        }

        [Test]
        public void RocketHitsAsteroid_ReversedOrder_BothGetDeadTag()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f), 5f);
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(asteroid, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag when asteroid is entityA");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag when asteroid is entityA");
        }

        [Test]
        public void RocketHitsShip_NoDeadTag()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f), 5f);
            var ship = CreateShipEntity(new float2(5f, 0f), 0f);

            AddCollisionEvent(rocket, ship);
            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should NOT get DeadTag on collision with ship");
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(ship),
                "Ship should NOT get DeadTag on collision with rocket");
        }

        [Test]
        public void RocketHitsPlayerBullet_NoDeadTag()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f), 5f);
            var bullet = CreateBulletEntity(
                new float2(5f, 0f), 20f, new float2(-1f, 0f), 2f, isPlayer: true);

            AddCollisionEvent(rocket, bullet);
            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should NOT get DeadTag on collision with player bullet");
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(bullet),
                "Player bullet should NOT get DeadTag on collision with rocket");
        }
    }
}
