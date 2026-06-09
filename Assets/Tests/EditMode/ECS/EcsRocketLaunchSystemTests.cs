using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsRocketLaunchSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _singletonEntity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsRocketLaunchSystem>();
            _singletonEntity = CreateRocketLaunchEventSingleton();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        private Entity CreateAmmoEntity(RocketAmmoData data)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, data);
            return entity;
        }

        [Test]
        public void Respawn_IncrementsCurrentRockets_ByOne()
        {
            var entity = CreateAmmoEntity(new RocketAmmoData
            {
                MaxRockets = 2,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 0,
                RespawnRemaining = 0.5f,
                Launching = false
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(1, ammo.CurrentRockets);
            Assert.AreEqual(5.0f, ammo.RespawnRemaining);
        }

        [Test]
        public void Respawn_DoesNotExceedMaxRockets()
        {
            var entity = CreateAmmoEntity(new RocketAmmoData
            {
                MaxRockets = 2,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 2,
                RespawnRemaining = 5.0f,
                Launching = false
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(2, ammo.CurrentRockets);
            // Таймер не тикает, пока боезапас полный
            Assert.AreEqual(5.0f, ammo.RespawnRemaining);
        }

        [Test]
        public void Launch_DecrementsCurrentRockets_AndEmitsEvent_WhenHasAmmo()
        {
            var entity = CreateAmmoEntity(new RocketAmmoData
            {
                MaxRockets = 2,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 2,
                RespawnRemaining = 5.0f,
                Launching = true
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(1, ammo.CurrentRockets);

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(entity, buffer[0].ShooterEntity);
        }

        [Test]
        public void Launch_DoesNothing_WhenNoAmmo()
        {
            CreateAmmoEntity(new RocketAmmoData
            {
                MaxRockets = 2,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 0,
                RespawnRemaining = 5.0f,
                Launching = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Launch_ResetsLaunching_AfterUpdate()
        {
            var entity = CreateAmmoEntity(new RocketAmmoData
            {
                MaxRockets = 2,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 1,
                RespawnRemaining = 5.0f,
                Launching = true
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.IsFalse(ammo.Launching);
        }

        [Test]
        public void Launch_RecordsCorrectPositionAndDirection()
        {
            CreateAmmoEntity(new RocketAmmoData
            {
                MaxRockets = 2,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 1,
                RespawnRemaining = 5.0f,
                Launching = true,
                LaunchPosition = new float2(3f, 4f),
                Direction = new float2(0f, 1f)
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(3f, 4f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void RespawnTimer_StartsTicking_AfterLaunch()
        {
            var entity = CreateAmmoEntity(new RocketAmmoData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 1,
                RespawnRemaining = 5.0f,
                Launching = true
            });

            RunSystem();
            // После запуска: 0 ракет, таймер начинает тикать
            RunSystem(2.0f);

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(0, ammo.CurrentRockets);
            Assert.AreEqual(3.0f, ammo.RespawnRemaining, 1e-3f);
        }
    }
}
