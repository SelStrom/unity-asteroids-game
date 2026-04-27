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

        private Entity CreateRocketShootEventSingleton()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<RocketShootEvent>(entity);
            return entity;
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Reload_RestoresOneShoot_AfterDuration()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 2,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.5f,
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
                MaxShoots = 2,
                ReloadDurationSec = 1.0f,
                CurrentShoots = 2,
                ReloadRemaining = 1.0f,
                Shooting = false
            });

            RunSystem();

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.AreEqual(2, rocket.CurrentShoots);
        }

        [Test]
        public void Shoot_DecrementsCurrentShoots_WhenShootingAndHasAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 2,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true,
                ShootPosition = new float2(1f, 2f),
                Direction = new float2(0f, 1f)
            });

            RunSystem();

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.AreEqual(0, rocket.CurrentShoots);

            var buffer = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
        }

        [Test]
        public void Shoot_DoesNothing_WhenShootingAndNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 2,
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
        public void Shoot_ResetsShooting_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 2,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.IsFalse(rocket.Shooting);
        }

        [Test]
        public void Shoot_RecordsCorrectPositionAndDirection()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 2,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true,
                ShootPosition = new float2(3f, 4f),
                Direction = new float2(1f, 0f)
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(3f, 4f), buffer[0].Position);
            Assert.AreEqual(new float2(1f, 0f), buffer[0].Direction);
        }

        [Test]
        public void Reload_DecrementsReloadRemaining()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketData
            {
                MaxShoots = 2,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 3.0f,
                Shooting = false
            });

            RunSystem(1.0f);

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.AreEqual(2.0f, rocket.ReloadRemaining, 0.001f);
            Assert.AreEqual(0, rocket.CurrentShoots);
        }
    }
}
