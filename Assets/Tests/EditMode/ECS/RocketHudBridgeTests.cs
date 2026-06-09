using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketHudBridgeTests : AsteroidsEcsTestFixture
    {
        private ObservableBridgeSystem _system;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.AddSystemManaged(new ObservableBridgeSystem());
        }

        [TearDown]
        public override void TearDown()
        {
            _system.ClearReferences();
            base.TearDown();
        }

        private Entity CreateShipWithLauncher(
            int currentRockets, int maxRockets, float respawnRemaining)
        {
            var entity = CreateShipEntity();
            m_Manager.SetComponentData(entity, new RocketLauncherData
            {
                MaxRockets = maxRockets,
                RespawnDurationSec = 10f,
                CurrentRockets = currentRockets,
                RespawnRemaining = respawnRemaining
            });
            return entity;
        }

        [Test]
        public void PushesRocketCount_ToHudData()
        {
            var hudData = new HudData();
            _system.SetHudData(hudData);

            CreateShipWithLauncher(currentRockets: 1, maxRockets: 1, respawnRemaining: 10f);

            _system.Update();

            Assert.IsTrue(hudData.RocketCount.Value.Contains("1"),
                "RocketCount should contain current rockets count");
        }

        [Test]
        public void RespawnTime_Visible_WhenNotFull()
        {
            var hudData = new HudData();
            _system.SetHudData(hudData);

            CreateShipWithLauncher(currentRockets: 0, maxRockets: 1, respawnRemaining: 7.2f);

            _system.Update();

            Assert.IsTrue(hudData.IsRocketRespawnTimeVisible.Value,
                "Respawn timer should be visible while rockets are respawning");
            Assert.IsTrue(hudData.RocketRespawnTime.Value.Contains("7"),
                "Respawn time should contain remaining seconds");
        }

        [Test]
        public void RespawnTime_Hidden_WhenFull()
        {
            var hudData = new HudData();
            _system.SetHudData(hudData);

            CreateShipWithLauncher(currentRockets: 1, maxRockets: 1, respawnRemaining: 10f);

            _system.Update();

            Assert.IsFalse(hudData.IsRocketRespawnTimeVisible.Value,
                "Respawn timer should be hidden when rockets are full");
        }
    }
}
