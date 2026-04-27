using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsMissileLauncherSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _eventBufferEntity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsMissileLauncherSystem>();
            _eventBufferEntity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<MissileShootEvent>(_eventBufferEntity);
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
            m_Manager.AddComponentData(entity, new MissileLauncherData
            {
                MaxShoots = 1,
                ReloadDurationSec = 1.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.1f,
                Shooting = false
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<MissileLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentShoots);
            Assert.AreEqual(1.0f, launcher.ReloadRemaining, 1e-4f);
        }

        [Test]
        public void Reload_DoesNotExceedMaxShoots()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileLauncherData
            {
                MaxShoots = 1,
                ReloadDurationSec = 1.0f,
                CurrentShoots = 1,
                ReloadRemaining = 1.0f,
                Shooting = false
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<MissileLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentShoots);
        }

        [Test]
        public void Shoot_DecrementsCurrentShoots_AndEmitsEvent()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileLauncherData
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

            var launcher = m_Manager.GetComponentData<MissileLauncherData>(entity);
            Assert.AreEqual(0, launcher.CurrentShoots);
            Assert.IsFalse(launcher.Shooting);

            var buffer = m_Manager.GetBuffer<MissileShootEvent>(_eventBufferEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(2f, 3f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void Shoot_EmitsNoEvent_WhenNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileLauncherData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<MissileShootEvent>(_eventBufferEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Shoot_ResetsShootingFlag_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileLauncherData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem(0f);

            var launcher = m_Manager.GetComponentData<MissileLauncherData>(entity);
            Assert.IsFalse(launcher.Shooting);
        }
    }
}
