using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketAmmoSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsRocketAmmoSystem>();
        }

        private Entity _singletonEntity;

        private void CreateRocketEventSingleton()
        {
            _singletonEntity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<RocketShootEvent>(_singletonEntity);
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Reload_IncrementsCurrentAmmo_ByOne()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 1.0f,
                CurrentAmmo = 1,
                ReloadRemaining = 0.1f
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(2, ammo.CurrentAmmo);
            Assert.AreEqual(1.0f, ammo.ReloadRemaining);
        }

        [Test]
        public void Reload_DoesNotExceedMaxAmmo()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 1.0f,
                CurrentAmmo = 3,
                ReloadRemaining = 1.0f
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(3, ammo.CurrentAmmo);
            Assert.AreEqual(1.0f, ammo.ReloadRemaining);
        }

        [Test]
        public void Reload_TimerDecreases_WhenNotFull()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 5.0f,
                CurrentAmmo = 1,
                ReloadRemaining = 2.0f
            });

            RunSystem(0.5f);

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(1.5f, ammo.ReloadRemaining, 0.001f);
            Assert.AreEqual(1, ammo.CurrentAmmo);
        }

        [Test]
        public void Reload_ResetsTimer_AfterReload()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 5.0f,
                CurrentAmmo = 0,
                ReloadRemaining = 0.1f
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(5.0f, ammo.ReloadRemaining, 0.001f);
            Assert.AreEqual(1, ammo.CurrentAmmo);
        }

        [Test]
        public void Reload_MultipleEntities_IndependentTimers()
        {
            var entityA = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entityA, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 2.0f,
                CurrentAmmo = 1,
                ReloadRemaining = 0.5f
            });

            var entityB = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entityB, new RocketAmmoData
            {
                MaxAmmo = 5,
                ReloadDurationSec = 10.0f,
                CurrentAmmo = 2,
                ReloadRemaining = 3.0f
            });

            RunSystem();

            var ammoA = m_Manager.GetComponentData<RocketAmmoData>(entityA);
            Assert.AreEqual(2, ammoA.CurrentAmmo);
            Assert.AreEqual(2.0f, ammoA.ReloadRemaining, 0.001f);

            var ammoB = m_Manager.GetComponentData<RocketAmmoData>(entityB);
            Assert.AreEqual(2, ammoB.CurrentAmmo);
            Assert.AreEqual(2.0f, ammoB.ReloadRemaining, 0.001f);
        }

        [Test]
        public void Shoot_WithAmmo_CreatesRocketShootEvent()
        {
            CreateRocketEventSingleton();

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 5.0f,
                CurrentAmmo = 2,
                ReloadRemaining = 5.0f,
                Shooting = true,
                Direction = new float2(0, 1),
                ShootPosition = new float2(1, 2)
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(1, ammo.CurrentAmmo);

            var buffer = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(entity, buffer[0].ShooterEntity);
            Assert.AreEqual(new float2(1, 2), buffer[0].Position);
            Assert.AreEqual(new float2(0, 1), buffer[0].Direction);
        }

        [Test]
        public void Shoot_WithoutAmmo_NoEvent()
        {
            CreateRocketEventSingleton();

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 5.0f,
                CurrentAmmo = 0,
                ReloadRemaining = 5.0f,
                Shooting = true,
                Direction = new float2(0, 1),
                ShootPosition = new float2(1, 2)
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(0, ammo.CurrentAmmo);

            var buffer = m_Manager.GetBuffer<RocketShootEvent>(_singletonEntity);
            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void Shoot_ResetsShootingFlag_Unconditionally()
        {
            CreateRocketEventSingleton();

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 5.0f,
                CurrentAmmo = 0,
                ReloadRemaining = 5.0f,
                Shooting = true,
                Direction = new float2(0, 1),
                ShootPosition = new float2(1, 2)
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.IsFalse(ammo.Shooting);
        }

        [Test]
        public void Shoot_WithAmmo_ResetsShootingFlag()
        {
            CreateRocketEventSingleton();

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 5.0f,
                CurrentAmmo = 2,
                ReloadRemaining = 5.0f,
                Shooting = true,
                Direction = new float2(0, 1),
                ShootPosition = new float2(1, 2)
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.IsFalse(ammo.Shooting);
        }

        [Test]
        public void Reload_StillWorks_WithShootingFields()
        {
            CreateRocketEventSingleton();

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketAmmoData
            {
                MaxAmmo = 3,
                ReloadDurationSec = 1.0f,
                CurrentAmmo = 1,
                ReloadRemaining = 0.1f,
                Shooting = false,
                Direction = new float2(1, 0),
                ShootPosition = new float2(0, 0)
            });

            RunSystem();

            var ammo = m_Manager.GetComponentData<RocketAmmoData>(entity);
            Assert.AreEqual(2, ammo.CurrentAmmo);
        }
    }
}
