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

        [Test]
        public void Shoot_DecrementsCurrentRockets_AndAddsEvent_WhenShootingAndHasAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 2.0f,
                CurrentRockets = 2,
                RespawnRemaining = 2.0f,
                Shooting = true,
                ShootPosition = new float2(1f, 2f),
                Direction = new float2(0f, 1f)
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.CurrentRockets);

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(1f, 2f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
            Assert.AreEqual(entity, buffer[0].ShooterEntity);
        }

        [Test]
        public void Shoot_DoesNotAddEvent_WhenShootingAndNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 2.0f,
                CurrentRockets = 0,
                RespawnRemaining = 2.0f,
                Shooting = true
            });

            RunSystem();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Respawn_IncrementsCurrentRockets_AndResetTimer_WhenTimerExpired()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 1.0f,
                CurrentRockets = 1,
                RespawnRemaining = 0.5f,
                Shooting = false
            });

            RunSystem(deltaTime: 1.0f);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(2, launcher.CurrentRockets);
            Assert.AreEqual(1.0f, launcher.RespawnRemaining);
        }

        [Test]
        public void Shooting_IsResetToFalse_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData
            {
                MaxRockets = 3,
                RespawnDurationSec = 2.0f,
                CurrentRockets = 2,
                RespawnRemaining = 2.0f,
                Shooting = true
            });

            RunSystem();

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.IsFalse(launcher.Shooting);
        }
    }
}
