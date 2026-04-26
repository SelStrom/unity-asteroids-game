using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EntityFactoryTests : AsteroidsEcsTestFixture
    {
        [Test]
        public void CreateShip_HasCorrectComponents()
        {
            var entity = EntityFactory.CreateShip(
                m_Manager,
                position: new float2(1f, 2f),
                moveSpeed: 5f,
                thrustAcceleration: 10f,
                thrustMaxSpeed: 15f,
                gunMaxShoots: 3,
                gunReloadSec: 1.5f,
                laserMaxShoots: 2,
                laserReloadSec: 5f,
                rocketMaxCount: 1,
                rocketRespawnSec: 4f
            );

            Assert.IsTrue(m_Manager.HasComponent<ShipTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RotateData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ThrustData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<GunData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LaserData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RocketLauncherData>(entity));
        }

        [Test]
        public void CreateShip_HasCorrectInitialValues()
        {
            var entity = EntityFactory.CreateShip(
                m_Manager,
                position: new float2(3f, 4f),
                moveSpeed: 7f,
                thrustAcceleration: 12f,
                thrustMaxSpeed: 20f,
                gunMaxShoots: 5,
                gunReloadSec: 2f,
                laserMaxShoots: 3,
                laserReloadSec: 6f,
                rocketMaxCount: 2,
                rocketRespawnSec: 8f
            );

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(new float2(3f, 4f), move.Position);
            Assert.AreEqual(7f, move.Speed);

            var rotate = m_Manager.GetComponentData<RotateData>(entity);
            Assert.AreEqual(new float2(1f, 0f), rotate.Rotation);

            var launcher = m_Manager.GetComponentData<RocketLauncherData>(entity);
            Assert.AreEqual(2, launcher.MaxRockets);
            Assert.AreEqual(8f, launcher.RespawnDurationSec);
            Assert.AreEqual(2, launcher.CurrentRockets);
            Assert.AreEqual(8f, launcher.RespawnRemaining);
            Assert.IsFalse(launcher.Launching);
        }

        [Test]
        public void CreateShip_DoesNotHaveScoreValue()
        {
            var entity = EntityFactory.CreateShip(
                m_Manager,
                position: default,
                moveSpeed: 0f,
                thrustAcceleration: 0f,
                thrustMaxSpeed: 0f,
                gunMaxShoots: 0,
                gunReloadSec: 0f,
                laserMaxShoots: 0,
                laserReloadSec: 0f,
                rocketMaxCount: 0,
                rocketRespawnSec: 0f
            );

            Assert.IsFalse(m_Manager.HasComponent<ScoreValue>(entity));
        }

        [Test]
        public void CreateRocket_HasAllRequiredComponents_AndCorrectValues()
        {
            var target = m_Manager.CreateEntity();
            var entity = EntityFactory.CreateRocket(
                m_Manager,
                position: new float2(2f, 3f),
                speed: 12f,
                direction: new float2(0f, 1f),
                turnRateRadPerSec: 2.5f,
                lifeTime: 6f,
                target: target
            );

            Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RocketHomingData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(new float2(2f, 3f), move.Position);
            Assert.AreEqual(12f, move.Speed);
            Assert.AreEqual(new float2(0f, 1f), move.Direction);

            var homing = m_Manager.GetComponentData<RocketHomingData>(entity);
            Assert.AreEqual(2.5f, homing.TurnRateRadPerSec);
            Assert.AreEqual(target, homing.TargetEntity);

            var life = m_Manager.GetComponentData<LifeTimeData>(entity);
            Assert.AreEqual(6f, life.TimeRemaining);
        }

        [Test]
        public void CreateAsteroid_HasCorrectComponents()
        {
            var entity = EntityFactory.CreateAsteroid(
                m_Manager,
                position: new float2(1f, 2f),
                speed: 3f,
                direction: new float2(0f, 1f),
                age: 3,
                score: 100
            );

            Assert.IsTrue(m_Manager.HasComponent<AsteroidTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<AgeData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ScoreValue>(entity));
        }

        [Test]
        public void CreateAsteroid_HasCorrectValues()
        {
            var entity = EntityFactory.CreateAsteroid(
                m_Manager,
                position: new float2(5f, 6f),
                speed: 4f,
                direction: new float2(1f, 0f),
                age: 2,
                score: 150
            );

            var age = m_Manager.GetComponentData<AgeData>(entity);
            Assert.AreEqual(2, age.Age);

            var scoreValue = m_Manager.GetComponentData<ScoreValue>(entity);
            Assert.AreEqual(150, scoreValue.Score);
        }

        [Test]
        public void CreateBullet_PlayerBullet_HasCorrectComponents()
        {
            var entity = EntityFactory.CreateBullet(
                m_Manager,
                position: new float2(1f, 2f),
                speed: 20f,
                direction: new float2(1f, 0f),
                lifeTime: 2f,
                isPlayer: true
            );

            Assert.IsTrue(m_Manager.HasComponent<BulletTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<PlayerBulletTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));
            Assert.IsFalse(m_Manager.HasComponent<EnemyBulletTag>(entity));
        }

        [Test]
        public void CreateBullet_EnemyBullet_HasCorrectComponents()
        {
            var entity = EntityFactory.CreateBullet(
                m_Manager,
                position: new float2(1f, 2f),
                speed: 20f,
                direction: new float2(1f, 0f),
                lifeTime: 2f,
                isPlayer: false
            );

            Assert.IsTrue(m_Manager.HasComponent<BulletTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<EnemyBulletTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));
            Assert.IsFalse(m_Manager.HasComponent<PlayerBulletTag>(entity));
        }

        [Test]
        public void CreateBullet_DoesNotHaveScoreValue()
        {
            var entity = EntityFactory.CreateBullet(
                m_Manager,
                position: default,
                speed: 0f,
                direction: default,
                lifeTime: 0f,
                isPlayer: true
            );

            Assert.IsFalse(m_Manager.HasComponent<ScoreValue>(entity));
        }

        [Test]
        public void CreateUfoBig_HasCorrectComponents()
        {
            var entity = EntityFactory.CreateUfoBig(
                m_Manager,
                position: new float2(1f, 2f),
                speed: 5f,
                direction: new float2(1f, 0f),
                gunMaxShoots: 3,
                gunReloadSec: 2f,
                score: 200
            );

            Assert.IsTrue(m_Manager.HasComponent<UfoBigTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<GunData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ShootToData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ScoreValue>(entity));

            var scoreValue = m_Manager.GetComponentData<ScoreValue>(entity);
            Assert.AreEqual(200, scoreValue.Score);
        }

        [Test]
        public void CreateUfo_HasCorrectComponents()
        {
            var entity = EntityFactory.CreateUfo(
                m_Manager,
                position: new float2(1f, 2f),
                speed: 5f,
                direction: new float2(1f, 0f),
                gunMaxShoots: 3,
                gunReloadSec: 2f,
                moveToEvery: 3f,
                score: 500
            );

            Assert.IsTrue(m_Manager.HasComponent<UfoTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<GunData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ShootToData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveToData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<ScoreValue>(entity));

            var scoreValue = m_Manager.GetComponentData<ScoreValue>(entity);
            Assert.AreEqual(500, scoreValue.Score);
        }
    }
}
