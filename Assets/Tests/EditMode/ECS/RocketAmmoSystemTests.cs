using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
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
    }
}
