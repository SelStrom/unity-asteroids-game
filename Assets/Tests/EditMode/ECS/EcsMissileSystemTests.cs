using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsMissileSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _singletonEntity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsMissileSystem>();
            _singletonEntity = CreateMissileSpawnEventSingleton();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Respawn_IncrementsCurrentShoots_ByOne()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 1.0f,
                CurrentShoots = 0,
                RespawnRemaining = 0.1f,
                Shooting = false
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(entity);
            Assert.AreEqual(1, missile.CurrentShoots);
            Assert.AreEqual(1.0f, missile.RespawnRemaining);
        }

        [Test]
        public void Respawn_DoesNotExceedMaxShoots()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 1.0f,
                CurrentShoots = 2,
                RespawnRemaining = 1.0f,
                Shooting = false
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(entity);
            Assert.AreEqual(2, missile.CurrentShoots);
        }

        [Test]
        public void Respawn_TicksDown_WhenBelowMax()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 5.0f,
                CurrentShoots = 1,
                RespawnRemaining = 5.0f,
                Shooting = false
            });

            RunSystem(2.0f);

            var missile = m_Manager.GetComponentData<MissileData>(entity);
            Assert.AreEqual(3.0f, missile.RespawnRemaining, 1e-4f);
            Assert.AreEqual(1, missile.CurrentShoots);
        }

        [Test]
        public void Shoot_DecrementsCurrentShoots_WhenShootingAndHasAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 5.0f,
                CurrentShoots = 1,
                RespawnRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(entity);
            Assert.AreEqual(0, missile.CurrentShoots);

            var buffer = m_Manager.GetBuffer<MissileSpawnEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
        }

        [Test]
        public void Shoot_DoesNothing_WhenShootingAndNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 5.0f,
                CurrentShoots = 0,
                RespawnRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<MissileSpawnEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Shoot_ResetsShooting_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 5.0f,
                CurrentShoots = 1,
                RespawnRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(entity);
            Assert.IsFalse(missile.Shooting);
        }

        [Test]
        public void Shoot_RecordsCorrectPositionAndDirection()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 5.0f,
                CurrentShoots = 1,
                RespawnRemaining = 5.0f,
                Shooting = true,
                ShootPosition = new float2(7f, -2f),
                Direction = new float2(0f, 1f)
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<MissileSpawnEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(7f, -2f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void Shoot_RecordsShooterEntity()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileData
            {
                MaxShoots = 2,
                RespawnDurationSec = 5.0f,
                CurrentShoots = 1,
                RespawnRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<MissileSpawnEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(entity, buffer[0].ShooterEntity);
        }
    }
}
