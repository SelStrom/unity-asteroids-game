using NUnit.Framework;
using Unity.Entities;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class LaserSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _entity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CreateAndGetSystem<EcsLaserSystem>();
            _entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 3,
                ReloadRemaining = 5.0f,
                Shooting = false
            });
        }

        [Test]
        public void ReloadRemaining_DecreasesBy_DeltaTime_WhenCurrentShoots_LessThan_MaxShoots()
        {
            m_Manager.SetComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 5.0f,
                Shooting = false
            });

            World.Update();

            var laser = m_Manager.GetComponentData<LaserData>(_entity);
            Assert.Less(laser.ReloadRemaining, 5.0f);
        }

        [Test]
        public void CurrentShoots_IncreasesBy1_WhenReloadRemaining_ReachesZero()
        {
            m_Manager.SetComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.01f,
                Shooting = false
            });

            World.Update();

            var laser = m_Manager.GetComponentData<LaserData>(_entity);
            // Инкрементальная перезарядка: +1, не до MaxShoots
            Assert.AreEqual(1, laser.CurrentShoots);
            Assert.AreEqual(5.0f, laser.ReloadRemaining);
        }

        [Test]
        public void ReloadRemaining_DoesNotDecrease_WhenCurrentShoots_EqualsMaxShoots()
        {
            m_Manager.SetComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 3,
                ReloadRemaining = 5.0f,
                Shooting = false
            });

            World.Update();

            var laser = m_Manager.GetComponentData<LaserData>(_entity);
            Assert.AreEqual(5.0f, laser.ReloadRemaining);
        }

        [Test]
        public void CurrentShoots_DecreasesBy1_WhenShooting_AndHasAmmo()
        {
            m_Manager.SetComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 3,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            World.Update();

            var laser = m_Manager.GetComponentData<LaserData>(_entity);
            Assert.AreEqual(2, laser.CurrentShoots);
        }

        [Test]
        public void CurrentShoots_StaysZero_WhenShooting_AndNoAmmo()
        {
            m_Manager.SetComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            World.Update();

            var laser = m_Manager.GetComponentData<LaserData>(_entity);
            Assert.AreEqual(0, laser.CurrentShoots);
        }

        [Test]
        public void Shooting_ResetToFalse_AfterUpdate()
        {
            m_Manager.SetComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 3,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            World.Update();

            var laser = m_Manager.GetComponentData<LaserData>(_entity);
            Assert.IsFalse(laser.Shooting);
        }

        [Test]
        public void IncrementalReload_After2Cycles_CurrentShoots_Equals2()
        {
            m_Manager.SetComponentData(_entity, new LaserData
            {
                MaxShoots = 3,
                UpdateDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.01f,
                Shooting = false
            });

            // Первый цикл перезарядки
            World.Update();

            var laser = m_Manager.GetComponentData<LaserData>(_entity);
            Assert.AreEqual(1, laser.CurrentShoots);

            // Устанавливаем ReloadRemaining близко к нулю для второго цикла
            laser.ReloadRemaining = 0.01f;
            m_Manager.SetComponentData(_entity, laser);

            // Второй цикл перезарядки
            World.Update();

            laser = m_Manager.GetComponentData<LaserData>(_entity);
            // После 2 циклов должно быть ровно 2, а не MaxShoots (3)
            Assert.AreEqual(2, laser.CurrentShoots);
        }
    }
}
