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
            _singletonEntity = CreateRocketLaunchEventSingleton();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        private Entity CreateLauncherEntity(RocketLauncherData data)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, data);
            return entity;
        }

        [Test]
        public void Launch_DecrementsCurrentRockets_AndEmitsEvent()
        {
            var entity = CreateLauncherEntity(new RocketLauncherData
            {
                MaxRockets = 1,
                CurrentRockets = 1,
                RespawnDurationSec = 5f,
                RespawnRemaining = 5f,
                Launching = true,
                LaunchPosition = new float2(3f, 4f),
                LaunchDirection = new float2(0f, 1f)
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, launcher.CurrentRockets);

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(3f, 4f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
            Assert.AreEqual(entity, buffer[0].ShooterEntity);
        }

        [Test]
        public void Launch_DoesNothing_WhenNoRockets()
        {
            var entity = CreateLauncherEntity(new RocketLauncherData
            {
                MaxRockets = 1,
                CurrentRockets = 0,
                RespawnDurationSec = 5f,
                RespawnRemaining = 5f,
                Launching = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
            Assert.AreEqual(0, m_Manager.GetComponentData<RocketLauncherData>(entity).CurrentRockets);
        }

        [Test]
        public void Launch_ResetsLaunching_AfterUpdate()
        {
            var entity = CreateLauncherEntity(new RocketLauncherData
            {
                MaxRockets = 1,
                CurrentRockets = 1,
                RespawnDurationSec = 5f,
                RespawnRemaining = 5f,
                Launching = true
            });

            RunSystem();

            Assert.IsFalse(m_Manager.GetComponentData<RocketLauncherData>(entity).Launching);
        }

        [Test]
        public void Respawn_RestoresOneRocket_AfterDuration()
        {
            var entity = CreateLauncherEntity(new RocketLauncherData
            {
                MaxRockets = 1,
                CurrentRockets = 0,
                RespawnDurationSec = 5f,
                RespawnRemaining = 0.5f
            });

            RunSystem(1.0f);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentRockets);
            Assert.AreEqual(5f, launcher.RespawnRemaining);
        }

        [Test]
        public void Respawn_TimerNotRunning_WhenFull()
        {
            var entity = CreateLauncherEntity(new RocketLauncherData
            {
                MaxRockets = 1,
                CurrentRockets = 1,
                RespawnDurationSec = 5f,
                RespawnRemaining = 5f
            });

            RunSystem(1.0f);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentRockets);
            Assert.AreEqual(5f, launcher.RespawnRemaining);
        }

        [Test]
        public void Respawn_TimerStarts_AfterLaunch()
        {
            var entity = CreateLauncherEntity(new RocketLauncherData
            {
                MaxRockets = 1,
                CurrentRockets = 1,
                RespawnDurationSec = 5f,
                RespawnRemaining = 5f,
                Launching = true
            });

            RunSystem(1.0f);
            // Запуск произошёл, ракет 0 — таймер начинает тикать со следующего кадра
            RunSystem(1.0f);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, launcher.CurrentRockets);
            Assert.AreEqual(4f, launcher.RespawnRemaining, 1e-4f);
        }

        [Test]
        public void Respawn_DoesNotExceedMaxRockets()
        {
            var entity = CreateLauncherEntity(new RocketLauncherData
            {
                MaxRockets = 2,
                CurrentRockets = 2,
                RespawnDurationSec = 5f,
                RespawnRemaining = 0.1f
            });

            RunSystem(10f);

            Assert.AreEqual(2, m_Manager.GetComponentData<RocketLauncherData>(entity).CurrentRockets);
        }
    }
}
