using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class CollisionHandlerRocketTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsCollisionHandlerSystem>();
            CreateScoreDataSingleton();
            CreateCollisionEventSingleton();
        }

        private void RunSystem()
        {
            _systemHandle.Update(World.Unmanaged);
        }

        private Entity CreateRocketEntity(float2 position)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketTag());
            m_Manager.AddComponentData(entity, new PlayerBulletTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = 8f,
                Direction = new float2(1f, 0f)
            });
            m_Manager.AddComponentData(entity, new HomingData { TurnSpeed = 180f });
            m_Manager.AddComponentData(entity, new LifeTimeData { TimeRemaining = 5f });
            return entity;
        }

        private void AddCollisionEvent(Entity a, Entity b)
        {
            var query = m_Manager.CreateEntityQuery(typeof(CollisionEventData));
            var bufferEntity = query.GetSingletonEntity();
            var buffer = m_Manager.GetBuffer<CollisionEventData>(bufferEntity);
            buffer.Add(new CollisionEventData { EntityA = a, EntityB = b });
        }

        [Test]
        public void Rocket_KillsAsteroid_AndAddScore()
        {
            var rocket = CreateRocketEntity(float2.zero);
            var asteroid = CreateAsteroidEntity(new float2(1f, 0f), 2f, new float2(-1f, 0f), 3, 100);

            AddCollisionEvent(rocket, asteroid);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));

            var scoreQuery = m_Manager.CreateEntityQuery(typeof(ScoreData));
            var score = m_Manager.GetComponentData<ScoreData>(scoreQuery.GetSingletonEntity());
            Assert.AreEqual(100, score.Value);
        }

        [Test]
        public void Rocket_KillsUfo_AndAddScore()
        {
            var rocket = CreateRocketEntity(float2.zero);
            var ufo = CreateUfoEntity(new float2(3f, 0f), 2f, new float2(-1f, 0f), 500);

            AddCollisionEvent(rocket, ufo);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo));

            var scoreQuery = m_Manager.CreateEntityQuery(typeof(ScoreData));
            var score = m_Manager.GetComponentData<ScoreData>(scoreQuery.GetSingletonEntity());
            Assert.AreEqual(500, score.Value);
        }

        [Test]
        public void Rocket_KillsUfoBig_AndAddScore()
        {
            var rocket = CreateRocketEntity(float2.zero);
            var ufoBig = CreateUfoBigEntity(new float2(3f, 0f), 2f, new float2(-1f, 0f), 200);

            AddCollisionEvent(rocket, ufoBig);
            RunSystem();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufoBig));

            var scoreQuery = m_Manager.CreateEntityQuery(typeof(ScoreData));
            var score = m_Manager.GetComponentData<ScoreData>(scoreQuery.GetSingletonEntity());
            Assert.AreEqual(200, score.Value);
        }

        [Test]
        public void Rocket_DoesNotKillShip()
        {
            var rocket = CreateRocketEntity(float2.zero);
            var ship = CreateShipEntity(new float2(1f, 0f));

            AddCollisionEvent(rocket, ship);
            RunSystem();

            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(ship));
        }
    }
}
