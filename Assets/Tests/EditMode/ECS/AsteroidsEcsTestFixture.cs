using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class AsteroidsEcsTestFixture
    {
        protected World World;
        protected EntityManager m_Manager;

        [SetUp]
        public virtual void SetUp()
        {
            World = new World("Test");
            m_Manager = World.EntityManager;
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (World != null && World.IsCreated)
            {
                World.Dispose();
            }

            World = null;
        }

        protected Entity CreateShipEntity(float2 position = default, float speed = 0f)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ShipTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed
            });
            m_Manager.AddComponentData(entity, new RotateData());
            m_Manager.AddComponentData(entity, new ThrustData());
            m_Manager.AddComponentData(entity, new GunData());
            m_Manager.AddComponentData(entity, new LaserData());
            m_Manager.AddComponentData(entity, new RocketData());
            return entity;
        }

        protected Entity CreateAsteroidEntity(
            float2 position, float speed, float2 direction, int age, int score = 100)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new AsteroidTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new AgeData
            {
                Age = age
            });
            m_Manager.AddComponentData(entity, new ScoreValue
            {
                Score = score
            });
            return entity;
        }

        protected Entity CreateBulletEntity(
            float2 position, float speed, float2 direction, float lifeTime, bool isPlayer)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new BulletTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new LifeTimeData
            {
                TimeRemaining = lifeTime
            });
            if (isPlayer)
            {
                m_Manager.AddComponentData(entity, new PlayerBulletTag());
            }
            else
            {
                m_Manager.AddComponentData(entity, new EnemyBulletTag());
            }
            return entity;
        }

        protected Entity CreateUfoBigEntity(
            float2 position, float speed, float2 direction, int score = 200)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new UfoBigTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new GunData());
            m_Manager.AddComponentData(entity, new ShootToData());
            m_Manager.AddComponentData(entity, new ScoreValue
            {
                Score = score
            });
            return entity;
        }

        protected Entity CreateUfoEntity(
            float2 position, float speed, float2 direction, int score = 500)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new UfoTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new GunData());
            m_Manager.AddComponentData(entity, new ShootToData());
            m_Manager.AddComponentData(entity, new MoveToData());
            m_Manager.AddComponentData(entity, new ScoreValue
            {
                Score = score
            });
            return entity;
        }

        protected Entity CreateGameAreaSingleton(float2 size)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GameAreaData
            {
                Size = size
            });
            return entity;
        }

        protected Entity CreateShipPositionSingleton(
            float2 position, float speed, float2 direction)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ShipPositionData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            return entity;
        }

        protected Entity CreateScoreDataSingleton(int value = 0)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ScoreData
            {
                Value = value
            });
            return entity;
        }

        protected Entity CreateCollisionEventSingleton()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<CollisionEventData>(entity);
            return entity;
        }

        protected T CreateAndGetSystem<T>() where T : unmanaged, ISystem
        {
            World.CreateSystem<T>();
            return World.Unmanaged.GetUnsafeSystemRef<T>(
                World.GetExistingSystem<T>()
            );
        }

        protected Entity CreateGunShootEventSingleton()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<GunShootEvent>(entity);
            return entity;
        }

        protected Entity CreateLaserShootEventSingleton()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<LaserShootEvent>(entity);
            return entity;
        }

        protected Entity CreateRocketShootEventSingleton()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<RocketShootEvent>(entity);
            return entity;
        }
    }
}
