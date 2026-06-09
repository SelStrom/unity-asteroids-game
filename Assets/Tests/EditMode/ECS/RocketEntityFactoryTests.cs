using NUnit.Framework;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketEntityFactoryTests : AsteroidsEcsTestFixture
    {
        [Test]
        public void CreateRocket_HasAllRequiredComponents()
        {
            var entity = EntityFactory.CreateRocket(
                m_Manager, new float2(1f, 2f), 8f, new float2(0f, 1f), 10f, 180f);

            Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<PlayerBulletTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RocketHomingData>(entity));
        }

        [Test]
        public void CreateRocket_InitializesData()
        {
            var entity = EntityFactory.CreateRocket(
                m_Manager, new float2(1f, 2f), 8f, new float2(0f, 1f), 10f, 180f);

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(new float2(1f, 2f), move.Position);
            Assert.AreEqual(8f, move.Speed);
            Assert.AreEqual(new float2(0f, 1f), move.Direction);

            Assert.AreEqual(10f, m_Manager.GetComponentData<LifeTimeData>(entity).TimeRemaining);

            var homing = m_Manager.GetComponentData<RocketHomingData>(entity);
            Assert.AreEqual(180f, homing.TurnRateDegPerSec);
            Assert.AreEqual(Unity.Entities.Entity.Null, homing.TargetEntity);
        }

        [Test]
        public void CreateShip_HasRocketLauncher()
        {
            var entity = EntityFactory.CreateShip(
                m_Manager, float2.zero, 0f, 10f, 15f, 3, 1f, 3, 5f, 1, 10f);

            Assert.IsTrue(m_Manager.HasComponent<RocketLauncherData>(entity));
            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(1, launcher.MaxRockets);
            Assert.AreEqual(1, launcher.CurrentRockets);
            Assert.AreEqual(10f, launcher.RespawnDurationSec);
            Assert.AreEqual(10f, launcher.RespawnRemaining);
            Assert.IsFalse(launcher.Launching);
        }
    }
}
