using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsGunSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _singletonEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CreateAndGetSystem<EcsGunSystem>();
            _singletonEntity = CreateGunShootEventSingleton();
        }

        [Test]
        public void Reload_RestoresCurrentShoots_AfterDuration()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 1.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.5f,
                Shooting = false
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(entity);
            Assert.AreEqual(5, gun.CurrentShoots);
            Assert.AreEqual(1.0f, gun.ReloadRemaining);
        }

        [Test]
        public void Shoot_DecrementsCurrentShoots_WhenShootingAndHasAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 3,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(entity);
            Assert.AreEqual(2, gun.CurrentShoots);

            var buffer = m_Manager.GetBuffer<GunShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
        }

        [Test]
        public void Shoot_DoesNothing_WhenShootingAndNoAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 0,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var buffer = m_Manager.GetBuffer<GunShootEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Shoot_SetsIsPlayerTrue_ForShipEntity()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new ShipTag());
            m_Manager.AddComponentData(entity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 1,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var buffer = m_Manager.GetBuffer<GunShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.IsTrue(buffer[0].IsPlayer);
        }

        [Test]
        public void Shoot_SetsIsPlayerFalse_ForNonShipEntity()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 1,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var buffer = m_Manager.GetBuffer<GunShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.IsFalse(buffer[0].IsPlayer);
        }

        [Test]
        public void Shoot_ResetsShooting_AfterUpdate()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 3,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(entity);
            Assert.IsFalse(gun.Shooting);
        }

        [Test]
        public void Shoot_RecordsCorrectPositionAndDirection()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 1,
                ReloadRemaining = 2.0f,
                Shooting = true,
                ShootPosition = new float2(1f, 2f),
                Direction = new float2(0f, 1f)
            });

            World.Update();

            var buffer = m_Manager.GetBuffer<GunShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new float2(1f, 2f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }
    }
}
