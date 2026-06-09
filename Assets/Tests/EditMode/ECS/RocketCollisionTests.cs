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

        // --- Rocket + Asteroid ---

        [Test]
        public void RocketHitsAsteroid_BothGetDeadTag()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag on collision with Asteroid");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag on collision with Rocket");
        }

        [Test]
        public void RocketHitsAsteroid_ScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(100, scoreData.Value,
                "Score should increase by Asteroid ScoreValue when Rocket hits Asteroid");
        }

        [Test]
        public void AsteroidHitsRocket_ReversedOrder_BothGetDeadTagAndScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(
                new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 150);

            AddCollisionEvent(asteroid, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag when Asteroid is entityA");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
                "Asteroid should get DeadTag when Asteroid is entityA");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(150, scoreData.Value,
                "Score should increase by Asteroid ScoreValue regardless of collision order");
        }

        // --- Rocket + Ufo ---

        [Test]
        public void RocketHitsUfo_BothGetDeadTagAndScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ufo = CreateUfoEntity(new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 500);

            AddCollisionEvent(rocket, ufo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag on collision with Ufo");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo),
                "Ufo should get DeadTag on collision with Rocket");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(500, scoreData.Value,
                "Score should increase by Ufo ScoreValue when Rocket hits Ufo");
        }

        [Test]
        public void UfoHitsRocket_ReversedOrder_BothGetDeadTagAndScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ufo = CreateUfoEntity(new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 500);

            AddCollisionEvent(ufo, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag when Ufo is entityA");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo),
                "Ufo should get DeadTag when Ufo is entityA");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(500, scoreData.Value,
                "Score should increase by Ufo ScoreValue regardless of collision order");
        }

        // --- Rocket + UfoBig ---

        [Test]
        public void RocketHitsUfoBig_BothGetDeadTagAndScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ufoBig = CreateUfoBigEntity(new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 200);

            AddCollisionEvent(rocket, ufoBig);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag on collision with UfoBig");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig),
                "UfoBig should get DeadTag on collision with Rocket");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(200, scoreData.Value,
                "Score should increase by UfoBig ScoreValue when Rocket hits UfoBig");
        }

        [Test]
        public void UfoBigHitsRocket_ReversedOrder_BothGetDeadTagAndScoreIncreased()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ufoBig = CreateUfoBigEntity(new float2(5f, 0f), 2f, new float2(-1f, 0f), score: 200);

            AddCollisionEvent(ufoBig, rocket);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should get DeadTag when UfoBig is entityA");
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig),
                "UfoBig should get DeadTag when UfoBig is entityA");

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(200, scoreData.Value,
                "Score should increase by UfoBig ScoreValue regardless of collision order");
        }

        // --- Rocket + Ship: дружественный огонь игнорируется ---

        [Test]
        public void RocketHitsShip_NeitherGetDeadTag()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ship = CreateShipEntity(new float2(5f, 0f), 0f);

            AddCollisionEvent(rocket, ship);
            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should NOT get DeadTag on collision with Ship (friendly fire)");
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(ship),
                "Ship should NOT get DeadTag on collision with Rocket (friendly fire)");
        }

        [Test]
        public void ShipHitsRocket_ReversedOrder_NeitherGetDeadTag()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ship = CreateShipEntity(new float2(5f, 0f), 0f);

            AddCollisionEvent(ship, rocket);
            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(rocket),
                "Rocket should NOT get DeadTag when Ship is entityA (friendly fire)");
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(ship),
                "Ship should NOT get DeadTag when Ship is entityA (friendly fire)");
        }

        [Test]
        public void RocketHitsShip_ScoreNotChanged()
        {
            var rocket = CreateRocketEntity(float2.zero, 15f, new float2(1f, 0f));
            var ship = CreateShipEntity(new float2(5f, 0f), 0f);

            AddCollisionEvent(rocket, ship);
            RunSystem();

            var scoreData = m_Manager.GetComponentData<ScoreData>(_scoreEntity);
            Assert.AreEqual(0, scoreData.Value,
                "Score should remain 0 when Rocket collides with Ship");
        }
    }
}
