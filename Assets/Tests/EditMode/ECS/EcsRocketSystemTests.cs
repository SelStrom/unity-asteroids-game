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
            _singletonEntity = CreateRocketLaunchEventSingleton();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Respawn_RestoresOneRocket_AfterDuration()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 0,
                RespawnRemaining = 0.5f,
                Launching = false
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentRockets);
            Assert.AreEqual(5.0f, launcher.RespawnRemaining);
        }

        [Test]
        public void Respawn_DoesNotExceedMax()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 1,
                RespawnRemaining = 0.1f,
                Launching = false
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentRockets);
            // При полном запасе таймер не должен тикать.
            Assert.AreEqual(0.1f, launcher.RespawnRemaining);
        }

        [Test]
        public void Launch_DecrementsCurrentRockets_AndAddsEvent()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 1,
                RespawnRemaining = 5.0f,
                Launching = true,
                LaunchPosition = new float2(2f, 3f),
                LaunchDirection = new float2(0f, 1f)
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, launcher.CurrentRockets);

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(2f, 3f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void Launch_DoesNothing_WhenNoRockets()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
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
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5.0f,
                CurrentRockets = 1,
                RespawnRemaining = 5.0f,
                Launching = true
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.IsFalse(launcher.Launching);
        }
    }
}
