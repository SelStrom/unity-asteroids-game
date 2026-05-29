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

        [Test]
        public void Reload_IncrementsCurrentRockets_ByOne()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 2,
                ReloadDurationSec = 1.0f,
                CurrentRockets = 0,
                ReloadRemaining = 0.1f,
                Launching = false
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, data.CurrentRockets);
            Assert.AreEqual(1.0f, data.ReloadRemaining);
        }

        [Test]
        public void Reload_DoesNotExceedMaxRockets()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                ReloadDurationSec = 1.0f,
                CurrentRockets = 1,
                ReloadRemaining = 1.0f,
                Launching = false
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, data.CurrentRockets);
        }

        [Test]
        public void Launch_DecrementsCurrentRockets_AndEmitsEvent()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                ReloadDurationSec = 10.0f,
                CurrentRockets = 1,
                ReloadRemaining = 10.0f,
                Launching = true
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, data.CurrentRockets);

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(1, buffer.Length);
        }

        [Test]
        public void Launch_DoesNothing_WhenNoRockets()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                ReloadDurationSec = 10.0f,
                CurrentRockets = 0,
                ReloadRemaining = 10.0f,
                Launching = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Launch_ResetsLaunching_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                ReloadDurationSec = 10.0f,
                CurrentRockets = 1,
                ReloadRemaining = 10.0f,
                Launching = true
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.IsFalse(data.Launching);
        }

        [Test]
        public void Launch_RecordsPositionAndDirection()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                ReloadDurationSec = 10.0f,
                CurrentRockets = 1,
                ReloadRemaining = 10.0f,
                Launching = true,
                LaunchPosition = new float2(3f, 4f),
                LaunchDirection = new float2(0f, 1f)
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(3f, 4f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void Reload_StartsAfterLaunch_WhenAtMaxBefore()
        {
            // Полный боезапас: респавн не тикает. После запуска ракет меньше максимума —
            // на следующем апдейте начинается перезарядка.
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                ReloadDurationSec = 10.0f,
                CurrentRockets = 1,
                ReloadRemaining = 10.0f,
                Launching = true
            });

            RunSystem(); // запуск -> CurrentRockets = 0
            RunSystem(2.0f); // респавн тикает: 10 - 2 = 8

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, data.CurrentRockets);
            Assert.AreEqual(8.0f, data.ReloadRemaining, 0.001f);
        }
    }
}
