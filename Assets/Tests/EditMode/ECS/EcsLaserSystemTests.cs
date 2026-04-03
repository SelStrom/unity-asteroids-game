using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsLaserSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _singletonEntity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsLaserSystem>();
            _singletonEntity = CreateLaserShootEventSingleton();
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
            m_Manager.AddComponentData(entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 1.0f,
                CurrentShoots = 1,
                ReloadRemaining = 0.1f,
                Shooting = false
            });

            RunSystem();

            var laser = m_Manager.GetComponentData<LaserData>(entity);
            Assert.AreEqual(2, laser.CurrentShoots);
            Assert.AreEqual(1.0f, laser.ReloadRemaining);
        }

        [Test]
        public void Reload_DoesNotExceedMaxShoots()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 1.0f,
                CurrentShoots = 3,
                ReloadRemaining = 1.0f,
                Shooting = false
            });

            RunSystem();

            var laser = m_Manager.GetComponentData<LaserData>(entity);
            Assert.AreEqual(3, laser.CurrentShoots);
        }

        [Test]
        public void Shoot_DecrementsCurrentShoots_WhenShootingAndHasAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 2.0f,
                CurrentShoots = 2,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            RunSystem();

            var laser = m_Manager.GetComponentData<LaserData>(entity);
            Assert.AreEqual(1, laser.CurrentShoots);

            var buffer = m_Manager.GetBuffer<LaserShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
        }

        [Test]
        public void Shoot_DoesNothing_WhenShootingAndNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 2.0f,
                CurrentShoots = 0,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<LaserShootEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Shoot_ResetsShooting_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 2.0f,
                CurrentShoots = 2,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            RunSystem();

            var laser = m_Manager.GetComponentData<LaserData>(entity);
            Assert.IsFalse(laser.Shooting);
        }

        [Test]
        public void Shoot_RecordsCorrectPositionAndDirection()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 2.0f,
                CurrentShoots = 1,
                ReloadRemaining = 2.0f,
                Shooting = true,
                ShootPosition = new float2(3f, 4f),
                Direction = new float2(1f, 0f)
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<LaserShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(3f, 4f), buffer[0].Position);
            Assert.AreEqual(new float2(1f, 0f), buffer[0].Direction);
        }
    }
}
