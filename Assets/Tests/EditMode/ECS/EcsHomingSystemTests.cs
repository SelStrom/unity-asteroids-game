using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsHomingSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsHomingSystem>();
            CreateGameAreaSingleton(new float2(20f, 20f));
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void HomingRocket_TurnsTowardNearestEnemy()
        {
            var asteroid = m_Manager.CreateEntity();
            m_Manager.AddComponentData(asteroid, new AsteroidTag());
            m_Manager.AddComponentData(asteroid, new MoveData
            {
                Position = new float2(5f, 0f),
                Speed = 1f,
                Direction = new float2(-1f, 0f)
            });

            var rocket = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rocket, new RocketTag());
            m_Manager.AddComponentData(rocket, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 8f,
                Direction = new float2(0f, 1f)
            });
            m_Manager.AddComponentData(rocket, new HomingData
            {
                TurnSpeed = 180f
            });

            RunSystem(0.1f);

            var moveData = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Greater(moveData.Direction.x, 0f);
        }

        [Test]
        public void HomingRocket_ChoosesNearestTarget()
        {
            var farAsteroid = m_Manager.CreateEntity();
            m_Manager.AddComponentData(farAsteroid, new AsteroidTag());
            m_Manager.AddComponentData(farAsteroid, new MoveData
            {
                Position = new float2(10f, 0f),
                Speed = 1f,
                Direction = new float2(-1f, 0f)
            });

            var nearAsteroid = m_Manager.CreateEntity();
            m_Manager.AddComponentData(nearAsteroid, new AsteroidTag());
            m_Manager.AddComponentData(nearAsteroid, new MoveData
            {
                Position = new float2(3f, 0f),
                Speed = 1f,
                Direction = new float2(-1f, 0f)
            });

            var rocket = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rocket, new RocketTag());
            m_Manager.AddComponentData(rocket, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 8f,
                Direction = new float2(0f, 1f)
            });
            m_Manager.AddComponentData(rocket, new HomingData
            {
                TurnSpeed = 180f
            });

            RunSystem(0.1f);

            var moveData = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Greater(moveData.Direction.x, 0f);
        }

        [Test]
        public void HomingRocket_TargetsUfo()
        {
            var ufo = m_Manager.CreateEntity();
            m_Manager.AddComponentData(ufo, new UfoTag());
            m_Manager.AddComponentData(ufo, new MoveData
            {
                Position = new float2(0f, 5f),
                Speed = 2f,
                Direction = new float2(1f, 0f)
            });

            var rocket = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rocket, new RocketTag());
            m_Manager.AddComponentData(rocket, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 8f,
                Direction = new float2(1f, 0f)
            });
            m_Manager.AddComponentData(rocket, new HomingData
            {
                TurnSpeed = 180f
            });

            RunSystem(0.1f);

            var moveData = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Greater(moveData.Direction.y, 0f);
        }

        [Test]
        public void HomingRocket_TargetsUfoBig()
        {
            var ufoBig = m_Manager.CreateEntity();
            m_Manager.AddComponentData(ufoBig, new UfoBigTag());
            m_Manager.AddComponentData(ufoBig, new MoveData
            {
                Position = new float2(-3f, 4f),
                Speed = 2f,
                Direction = new float2(1f, 0f)
            });

            var rocket = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rocket, new RocketTag());
            m_Manager.AddComponentData(rocket, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 8f,
                Direction = new float2(1f, 0f)
            });
            m_Manager.AddComponentData(rocket, new HomingData
            {
                TurnSpeed = 180f
            });

            RunSystem(0.1f);

            var moveData = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Greater(moveData.Direction.y, 0f);
        }

        [Test]
        public void HomingRocket_KeepsDirection_WhenNoTargets()
        {
            var rocket = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rocket, new RocketTag());
            m_Manager.AddComponentData(rocket, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 8f,
                Direction = new float2(0f, 1f)
            });
            m_Manager.AddComponentData(rocket, new HomingData
            {
                TurnSpeed = 180f
            });

            RunSystem(0.1f);

            var moveData = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, moveData.Direction.x, 0.001f);
            Assert.AreEqual(1f, moveData.Direction.y, 0.001f);
        }

        [Test]
        public void HomingRocket_TurnSpeedLimitsRotation()
        {
            var asteroid = m_Manager.CreateEntity();
            m_Manager.AddComponentData(asteroid, new AsteroidTag());
            m_Manager.AddComponentData(asteroid, new MoveData
            {
                Position = new float2(0f, -5f),
                Speed = 1f,
                Direction = new float2(0f, 1f)
            });

            var rocket = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rocket, new RocketTag());
            m_Manager.AddComponentData(rocket, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 8f,
                Direction = new float2(0f, 1f)
            });
            m_Manager.AddComponentData(rocket, new HomingData
            {
                TurnSpeed = 90f
            });

            RunSystem(0.1f);

            var moveData = m_Manager.GetComponentData<MoveData>(rocket);
            var angle = math.degrees(math.atan2(moveData.Direction.y, moveData.Direction.x));
            Assert.Greater(angle, 70f);
        }

        [Test]
        public void HomingRocket_DoesNotTargetDeadEntities()
        {
            var asteroid = m_Manager.CreateEntity();
            m_Manager.AddComponentData(asteroid, new AsteroidTag());
            m_Manager.AddComponentData(asteroid, new MoveData
            {
                Position = new float2(5f, 0f),
                Speed = 1f,
                Direction = new float2(-1f, 0f)
            });
            m_Manager.AddComponentData(asteroid, new DeadTag());

            var rocket = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rocket, new RocketTag());
            m_Manager.AddComponentData(rocket, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 8f,
                Direction = new float2(0f, 1f)
            });
            m_Manager.AddComponentData(rocket, new HomingData
            {
                TurnSpeed = 180f
            });

            RunSystem(0.1f);

            var moveData = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, moveData.Direction.x, 0.001f);
            Assert.AreEqual(1f, moveData.Direction.y, 0.001f);
        }
    }
}
