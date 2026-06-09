using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EntityFactoryRocketTests : AsteroidsEcsTestFixture
    {
        private Entity CreateShipWithRockets(int rocketMaxCount = 1, float rocketRespawnSec = 10f)
        {
            return EntityFactory.CreateShip(
                m_Manager,
                position: default,
                moveSpeed: 0f,
                thrustAcceleration: 0f,
                thrustMaxSpeed: 0f,
                gunMaxShoots: 0,
                gunReloadSec: 0f,
                laserMaxShoots: 0,
                laserReloadSec: 0f,
                rocketMaxCount: rocketMaxCount,
                rocketRespawnSec: rocketRespawnSec
            );
        }

        [Test]
        public void CreateShip_HasRocketAmmo_WithFullLoadout()
        {
            var entity = CreateShipWithRockets(rocketMaxCount: 2, rocketRespawnSec: 7f);

            Assert.IsTrue(m_Manager.HasComponent<RocketAmmoData>(entity));
            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(2, ammo.MaxRockets);
            Assert.AreEqual(2, ammo.CurrentRockets);
            Assert.AreEqual(7f, ammo.RespawnDurationSec);
            Assert.AreEqual(7f, ammo.RespawnRemaining);
            Assert.IsFalse(ammo.Launching);
        }

        [Test]
        public void CreateRocket_HasCorrectComponents()
        {
            var entity = EntityFactory.CreateRocket(
                m_Manager,
                position: new float2(1f, 2f),
                speed: 8f,
                direction: new float2(0f, 1f),
                lifeTime: 6f,
                turnSpeedDegPerSec: 120f
            );

            Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<PlayerBulletTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RotateData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RocketData>(entity));
        }

        [Test]
        public void CreateRocket_HasCorrectInitialValues()
        {
            var direction = new float2(0f, 1f);
            var entity = EntityFactory.CreateRocket(
                m_Manager,
                position: new float2(1f, 2f),
                speed: 8f,
                direction: direction,
                lifeTime: 6f,
                turnSpeedDegPerSec: 120f
            );

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(new float2(1f, 2f), move.Position);
            Assert.AreEqual(8f, move.Speed);
            Assert.AreEqual(direction, move.Direction);

            var rotate = m_Manager.GetComponentData<RotateData>(entity);
            Assert.AreEqual(direction, rotate.Rotation);

            var lifeTime = m_Manager.GetComponentData<LifeTimeData>(entity);
            Assert.AreEqual(6f, lifeTime.TimeRemaining);

            var rocket = m_Manager.GetComponentData<RocketData>(entity);
            Assert.AreEqual(Entity.Null, rocket.TargetEntity);
            Assert.AreEqual(120f, rocket.TurnSpeedDegPerSec);
        }

        [Test]
        public void CreateRocket_DiesByLifeTime()
        {
            var entity = EntityFactory.CreateRocket(
                m_Manager,
                position: default,
                speed: 8f,
                direction: new float2(1f, 0f),
                lifeTime: 0.5f,
                turnSpeedDegPerSec: 120f
            );

            var lifeTimeSystem = World.CreateSystem<EcsLifeTimeSystem>();
            var deadSystem = World.CreateSystem<EcsDeadByLifeTimeSystem>();
            World.PushTime(new Unity.Core.TimeData(1.0, 1.0f));
            lifeTimeSystem.Update(World.Unmanaged);
            deadSystem.Update(World.Unmanaged);
            World.PopTime();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(entity));
        }
    }
}
