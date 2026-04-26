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
        public void Reload_RestoresOneRocket_AfterRespawnDuration()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 2f,
                CurrentRockets = 0,
                RespawnRemaining = 0.5f,
                Launching = false
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, data.CurrentRockets);
            Assert.AreEqual(2f, data.RespawnRemaining);
        }

        [Test]
        public void Reload_DoesNothing_WhenAtMax()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 2f,
                CurrentRockets = 1,
                RespawnRemaining = 2f,
                Launching = false
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, data.CurrentRockets);
            Assert.AreEqual(2f, data.RespawnRemaining,
                "Таймер не должен тикать при полном боезапасе");
        }

        [Test]
        public void Reload_TicksDown_WhenBelowMax_AndNotReady()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5f,
                CurrentRockets = 0,
                RespawnRemaining = 5f,
                Launching = false
            });

            RunSystem(1f);

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, data.CurrentRockets);
            Assert.AreEqual(4f, data.RespawnRemaining);
        }

        [Test]
        public void Launch_DecrementsCurrentRockets_AndAddsEvent()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5f,
                CurrentRockets = 1,
                RespawnRemaining = 5f,
                Launching = true,
                LaunchPosition = new float2(3f, 4f),
                LaunchDirection = new float2(0f, 1f)
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, data.CurrentRockets);

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(entity, buffer[0].ShooterEntity);
            Assert.AreEqual(new float2(3f, 4f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void Launch_StartsCooldown_WhenAmmoDropsBelowMax()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 4f,
                CurrentRockets = 1,
                RespawnRemaining = 4f,
                Launching = true,
                LaunchPosition = new float2(1f, 0f),
                LaunchDirection = new float2(1f, 0f)
            });

            RunSystem(0f);

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(0, data.CurrentRockets);
            Assert.AreEqual(4f, data.RespawnRemaining,
                "После выстрела таймер должен сброситься на полную длительность");
        }

        [Test]
        public void Launch_DoesNothing_WhenNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5f,
                CurrentRockets = 0,
                RespawnRemaining = 5f,
                Launching = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Launch_ResetsLaunchingFlag_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5f,
                CurrentRockets = 1,
                RespawnRemaining = 5f,
                Launching = true
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.IsFalse(data.Launching);
        }

        [Test]
        public void Launch_ResetsLaunchingFlag_EvenWithoutAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 1,
                RespawnDurationSec = 5f,
                CurrentRockets = 0,
                RespawnRemaining = 5f,
                Launching = true
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.IsFalse(data.Launching);
        }

        [Test]
        public void MultipleRockets_RestoreOneByOne()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 1f,
                CurrentRockets = 0,
                RespawnRemaining = 0f,
                Launching = false
            });

            RunSystem();

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, data.CurrentRockets, "Сначала +1 ракета (как у лазера)");
            Assert.AreEqual(1f, data.RespawnRemaining);
        }
    }
}
