using NUnit.Framework;
using Unity.Entities;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class GunSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _entity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CreateAndGetSystem<EcsGunSystem>();
            _entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(_entity, new GunData
            {
                MaxShoots = 3,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 3,
                ReloadRemaining = 2.0f,
                Shooting = false
            });
        }

        [Test]
        public void ReloadRemaining_DecreasesBy_DeltaTime_WhenCurrentShoots_LessThan_MaxShoots()
        {
            m_Manager.SetComponentData(_entity, new GunData
            {
                MaxShoots = 3,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 0,
                ReloadRemaining = 2.0f,
                Shooting = false
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(_entity);
            // deltaTime зависит от World.Time, но ReloadRemaining должен уменьшиться
            Assert.Less(gun.ReloadRemaining, 2.0f);
        }

        [Test]
        public void CurrentShoots_ResetsTo_MaxShoots_WhenReloadRemaining_ReachesZero()
        {
            m_Manager.SetComponentData(_entity, new GunData
            {
                MaxShoots = 3,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.01f,
                Shooting = false
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(_entity);
            Assert.AreEqual(3, gun.CurrentShoots);
            Assert.AreEqual(2.0f, gun.ReloadRemaining);
        }

        [Test]
        public void ReloadRemaining_DoesNotDecrease_WhenCurrentShoots_EqualsMaxShoots()
        {
            m_Manager.SetComponentData(_entity, new GunData
            {
                MaxShoots = 3,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 3,
                ReloadRemaining = 2.0f,
                Shooting = false
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(_entity);
            Assert.AreEqual(2.0f, gun.ReloadRemaining);
        }

        [Test]
        public void CurrentShoots_DecreasesBy1_WhenShooting_AndHasAmmo()
        {
            m_Manager.SetComponentData(_entity, new GunData
            {
                MaxShoots = 3,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 3,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(_entity);
            Assert.AreEqual(2, gun.CurrentShoots);
        }

        [Test]
        public void CurrentShoots_StaysZero_WhenShooting_AndNoAmmo()
        {
            m_Manager.SetComponentData(_entity, new GunData
            {
                MaxShoots = 3,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 0,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(_entity);
            Assert.AreEqual(0, gun.CurrentShoots);
        }

        [Test]
        public void Shooting_ResetToFalse_AfterUpdate()
        {
            m_Manager.SetComponentData(_entity, new GunData
            {
                MaxShoots = 3,
                ReloadDurationSec = 2.0f,
                CurrentShoots = 3,
                ReloadRemaining = 2.0f,
                Shooting = true
            });

            World.Update();

            var gun = m_Manager.GetComponentData<GunData>(_entity);
            Assert.IsFalse(gun.Shooting);
        }
    }
}
