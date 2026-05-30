using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsRocketLauncherSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _eventSingleton;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsRocketLauncherSystem>();
            _eventSingleton = CreateRocketLaunchEventSingleton();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        private Entity CreateLauncher(RocketLauncherData data)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ShipTag());
            m_Manager.AddComponentData(entity, data);
            return entity;
        }

        [Test]
        public void Respawn_AddsOneRocket_AfterDuration()
        {
            var entity = CreateLauncher(new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 2f,
                CurrentRockets = 1,
                RespawnRemaining = 0.5f
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(2, data.CurrentRockets);
            Assert.AreEqual(2f, data.RespawnRemaining, 0.0001f);
        }

        [Test]
        public void Respawn_DoesNotExceedMax()
        {
            var entity = CreateLauncher(new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 2f,
                CurrentRockets = 1,
                RespawnRemaining = 0.1f
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, data.CurrentRockets);
        }

        [Test]
        public void Launch_EmitsEvent_AndDecrementsRockets()
        {
            var entity = CreateLauncher(new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 2f,
                CurrentRockets = 2,
                RespawnRemaining = 2f,
                Launching = true,
                LaunchPosition = new float2(1f, 2f),
                LaunchDirection = new float2(0f, 1f)
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, data.CurrentRockets);
            Assert.IsFalse(data.Launching, "Launching must reset after update");

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(1f, 2f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void Launch_DoesNothing_WhenNoRockets()
        {
            var entity = CreateLauncher(new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 2f,
                CurrentRockets = 0,
                RespawnRemaining = 2f,
                Launching = true,
                LaunchDirection = new float2(1f, 0f)
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, data.CurrentRockets);

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(0, buffer.Length);
        }
    }
}
