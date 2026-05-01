using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsRocketSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _singletonEntity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsRocketSystem>();
            _singletonEntity = CreateRocketShootEventSingleton();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Reload_IncrementsCurrentShoots_ByOne()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.1f,
                Shooting = false
            });

            RunSystem();

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.AreEqual(1, rocket.CurrentShoots);
            Assert.AreEqual(5.0f, rocket.ReloadRemaining);
        }

        [Test]
        public void Reload_DoesNotExceedMaxShoots()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 1.0f,
                Shooting = false
            });

            RunSystem();

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.AreEqual(1, rocket.CurrentShoots);
        }

        [Test]
        public void Shoot_DecrementsCurrentShoots_AndEmitsEvent()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true,
                ShootPosition = new float2(2f, 3f),
                Direction = new float2(0f, 1f)
            });

            RunSystem();

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.AreEqual(0, rocket.CurrentShoots);
            Assert.IsFalse(rocket.Shooting);

            var buffer = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(2f, 3f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void Shoot_DoesNothing_WhenNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Shoot_ResetsShootingFlag()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.IsFalse(rocket.Shooting);
        }
    }
}
