using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsRocketLauncherSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _singletonEntity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsRocketLauncherSystem>();
            _singletonEntity = CreateRocketShootEventSingleton();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Launch_AddsEvent_AndDecrementsRockets()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 10f,
                CurrentRockets = 1,
                RespawnRemaining = 10f,
                Launching = true,
                Direction = new float2(0f, 1f),
                LaunchPosition = new float2(2f, 3f)
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, launcher.CurrentRockets);

            var events = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(entity, events[0].ShooterEntity);
            Assert.AreEqual(new float2(2f, 3f), events[0].Position);
            Assert.AreEqual(new float2(0f, 1f), events[0].Direction);
        }

        [Test]
        public void Launch_WithoutAmmo_DoesNothing()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 10f,
                CurrentRockets = 0,
                RespawnRemaining = 10f,
                Launching = true
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, launcher.CurrentRockets);

            var events = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(0, events.Length);
        }

        [Test]
        public void Launch_FlagIsReset_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 10f,
                CurrentRockets = 1,
                RespawnRemaining = 10f,
                Launching = true
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.IsFalse(launcher.Launching);
        }

        [Test]
        public void Respawn_RestoresOneRocket_AfterDuration()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 2,
                RespawnDurationSec = 10f,
                CurrentRockets = 0,
                RespawnRemaining = 0.5f,
                Launching = false
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentRockets);
            Assert.AreEqual(10f, launcher.RespawnRemaining);
        }

        [Test]
        public void Respawn_TimerDecreases_WhenNotFull()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 10f,
                CurrentRockets = 0,
                RespawnRemaining = 10f,
                Launching = false
            });

            RunSystem(2f);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, launcher.CurrentRockets);
            Assert.AreEqual(8f, launcher.RespawnRemaining, 1e-4f);
        }

        [Test]
        public void Respawn_DoesNotTick_WhenFull()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 10f,
                CurrentRockets = 1,
                RespawnRemaining = 10f,
                Launching = false
            });

            RunSystem(3f);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentRockets);
            Assert.AreEqual(10f, launcher.RespawnRemaining);
        }

        [Test]
        public void Respawn_DoesNotExceedMax()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 2,
                RespawnDurationSec = 1f,
                CurrentRockets = 1,
                RespawnRemaining = 0.5f,
                Launching = false
            });

            RunSystem();
            RunSystem();
            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(2, launcher.CurrentRockets);
        }

        [Test]
        public void LaunchAndRespawn_FullCycle()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 2f,
                CurrentRockets = 1,
                RespawnRemaining = 2f,
                Launching = true
            });

            RunSystem(0.1f);
            Assert.AreEqual(0,
                m_Manager.GetComponentData<RocketLauncherData>(entity).CurrentRockets);

            RunSystem(1f);
            Assert.AreEqual(0,
                m_Manager.GetComponentData<RocketLauncherData>(entity).CurrentRockets);

            RunSystem(1.5f);
            Assert.AreEqual(1,
                m_Manager.GetComponentData<RocketLauncherData>(entity).CurrentRockets);
        }
    }
}
