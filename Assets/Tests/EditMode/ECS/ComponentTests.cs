using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    [TestFixture]
    public class ComponentTests : AsteroidsEcsTestFixture
    {
        [Test]
        public void CreateEntity_WithMoveData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveData());

            var data = m_Manager.GetComponentData<MoveData>(entity);

            Assert.AreEqual(float2.zero, data.Position);
            Assert.AreEqual(0f, data.Speed);
            Assert.AreEqual(float2.zero, data.Direction);
        }

        [Test]
        public void CreateEntity_WithThrustData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ThrustData());

            var data = m_Manager.GetComponentData<ThrustData>(entity);

            Assert.AreEqual(0f, ThrustData.MinSpeed);
            Assert.AreEqual(0f, data.UnitsPerSecond);
            Assert.AreEqual(0f, data.MaxSpeed);
            Assert.IsFalse(data.IsActive);
        }

        [Test]
        public void CreateEntity_WithRotateData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RotateData());

            var data = m_Manager.GetComponentData<RotateData>(entity);

            Assert.AreEqual(90f, RotateData.DegreePerSecond);
            Assert.AreEqual(0f, data.TargetDirection);
            Assert.AreEqual(float2.zero, data.Rotation);
        }

        [Test]
        public void CreateEntity_WithGunData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GunData());

            var data = m_Manager.GetComponentData<GunData>(entity);

            Assert.AreEqual(0, data.CurrentShoots);
            Assert.IsFalse(data.Shooting);
            Assert.AreEqual(0f, data.ReloadRemaining);
            Assert.AreEqual(0, data.MaxShoots);
        }

        [Test]
        public void CreateEntity_WithLaserData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new LaserData());

            var data = m_Manager.GetComponentData<LaserData>(entity);

            Assert.AreEqual(0, data.CurrentShoots);
            Assert.AreEqual(0f, data.ReloadRemaining);
            Assert.AreEqual(0, data.MaxShoots);
            Assert.IsFalse(data.Shooting);
        }

        [Test]
        public void CreateEntity_WithShootToData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ShootToData());
            Assert.IsTrue(m_Manager.HasComponent<ShootToData>(entity));
        }

        [Test]
        public void CreateEntity_WithMoveToData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveToData());

            var data = m_Manager.GetComponentData<MoveToData>(entity);

            Assert.AreEqual(0f, data.Every);
            Assert.AreEqual(0f, data.ReadyRemaining);
        }

        [Test]
        public void CreateEntity_WithLifeTimeData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new LifeTimeData());

            var data = m_Manager.GetComponentData<LifeTimeData>(entity);

            Assert.AreEqual(0f, data.TimeRemaining);
        }

        [Test]
        public void CreateEntity_WithAgeData_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new AgeData());

            var data = m_Manager.GetComponentData<AgeData>(entity);

            Assert.AreEqual(0, data.Age);
        }

        [Test]
        public void CreateEntity_WithScoreValue_DefaultValuesCorrect()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ScoreValue());

            var data = m_Manager.GetComponentData<ScoreValue>(entity);

            Assert.AreEqual(0, data.Score);
        }

        [Test]
        public void CreateEntity_WithTagComponents_HasComponentReturnsTrue()
        {
            var ship = m_Manager.CreateEntity();
            m_Manager.AddComponentData(ship, new ShipTag());

            var asteroid = m_Manager.CreateEntity();
            m_Manager.AddComponentData(asteroid, new AsteroidTag());

            var bullet = m_Manager.CreateEntity();
            m_Manager.AddComponentData(bullet, new BulletTag());

            var ufo = m_Manager.CreateEntity();
            m_Manager.AddComponentData(ufo, new UfoTag());

            var ufoBig = m_Manager.CreateEntity();
            m_Manager.AddComponentData(ufoBig, new UfoBigTag());

            var playerBullet = m_Manager.CreateEntity();
            m_Manager.AddComponentData(playerBullet, new PlayerBulletTag());

            var enemyBullet = m_Manager.CreateEntity();
            m_Manager.AddComponentData(enemyBullet, new EnemyBulletTag());

            var dead = m_Manager.CreateEntity();
            m_Manager.AddComponentData(dead, new DeadTag());

            Assert.IsTrue(m_Manager.HasComponent<ShipTag>(ship));
            Assert.IsTrue(m_Manager.HasComponent<AsteroidTag>(asteroid));
            Assert.IsTrue(m_Manager.HasComponent<BulletTag>(bullet));
            Assert.IsTrue(m_Manager.HasComponent<UfoTag>(ufo));
            Assert.IsTrue(m_Manager.HasComponent<UfoBigTag>(ufoBig));
            Assert.IsTrue(m_Manager.HasComponent<PlayerBulletTag>(playerBullet));
            Assert.IsTrue(m_Manager.HasComponent<EnemyBulletTag>(enemyBullet));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(dead));
        }

        [Test]
        public void CreateEntity_WithGameAreaData_SingletonReturnsCorrectSize()
        {
            var size = new float2(80f, 45f);
            CreateGameAreaSingleton(size);

            var query = m_Manager.CreateEntityQuery(typeof(GameAreaData));
            var data = query.GetSingleton<GameAreaData>();

            Assert.AreEqual(size.x, data.Size.x);
            Assert.AreEqual(size.y, data.Size.y);
        }

        [Test]
        public void CreateEntity_WithShipPositionData_SingletonReturnsCorrectValues()
        {
            var position = new float2(10f, 20f);
            var speed = 5f;
            var direction = new float2(1f, 0f);
            CreateShipPositionSingleton(position, speed, direction);

            var query = m_Manager.CreateEntityQuery(typeof(ShipPositionData));
            var data = query.GetSingleton<ShipPositionData>();

            Assert.AreEqual(position.x, data.Position.x);
            Assert.AreEqual(position.y, data.Position.y);
            Assert.AreEqual(speed, data.Speed);
            Assert.AreEqual(direction.x, data.Direction.x);
            Assert.AreEqual(direction.y, data.Direction.y);
        }

        [Test]
        public void CreateEntity_WithScoreData_SingletonReturnsDefaultValue()
        {
            CreateScoreDataSingleton();

            var query = m_Manager.CreateEntityQuery(typeof(ScoreData));
            var data = query.GetSingleton<ScoreData>();

            Assert.AreEqual(0, data.Value);
        }

        [Test]
        public void CreateEntity_WithCollisionEventBuffer_CanAddElement()
        {
            var entity = CreateCollisionEventSingleton();

            var buffer = m_Manager.GetBuffer<CollisionEventData>(entity);
            var entityA = m_Manager.CreateEntity();
            var entityB = m_Manager.CreateEntity();
            buffer.Add(new CollisionEventData
            {
                EntityA = entityA,
                EntityB = entityB
            });

            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(entityA, buffer[0].EntityA);
            Assert.AreEqual(entityB, buffer[0].EntityB);
        }

        [Test]
        public void CreateShipEntity_HasAllRequiredComponents()
        {
            var position = new float2(5f, 10f);
            var entity = CreateShipEntity(position, 3f);

            Assert.IsTrue(m_Manager.HasComponent<ShipTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RotateData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ThrustData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<GunData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LaserData>(entity));

            var moveData = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(position.x, moveData.Position.x);
            Assert.AreEqual(position.y, moveData.Position.y);
            Assert.AreEqual(3f, moveData.Speed);
        }

        [Test]
        public void CreateAsteroidEntity_HasScoreValue()
        {
            var entity = CreateAsteroidEntity(
                new float2(1f, 2f), 5f, new float2(0f, 1f), 3, 150);

            Assert.IsTrue(m_Manager.HasComponent<AsteroidTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ScoreValue>(entity));

            var score = m_Manager.GetComponentData<ScoreValue>(entity);
            Assert.AreEqual(150, score.Score);

            var age = m_Manager.GetComponentData<AgeData>(entity);
            Assert.AreEqual(3, age.Age);
        }

        [Test]
        public void CreateUfoEntity_HasScoreValueAndMoveToData()
        {
            var entity = CreateUfoEntity(
                new float2(0f, 0f), 4f, new float2(1f, 0f), 500);

            Assert.IsTrue(m_Manager.HasComponent<UfoTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveToData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ScoreValue>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ShootToData>(entity));

            var score = m_Manager.GetComponentData<ScoreValue>(entity);
            Assert.AreEqual(500, score.Score);
        }
    }
}
