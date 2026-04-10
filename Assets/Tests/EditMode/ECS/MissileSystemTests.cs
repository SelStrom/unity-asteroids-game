using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class MissileSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _entity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsMissileSystem>();
            CreateMissileShootEventSingleton();
            _entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = false
            });
        }

        private Entity CreateMissileShootEventSingleton()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<MissileShootEvent>(entity);
            return entity;
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void ReloadRemaining_DecreasesBy_DeltaTime_WhenCurrentShoots_LessThan_MaxShoots()
        {
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 5.0f,
                Shooting = false
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(_entity);
            Assert.AreEqual(4.0f, missile.ReloadRemaining, 0.01f);
        }

        [Test]
        public void CurrentShoots_IncrementsBy1_WhenReloadRemaining_ReachesZero()
        {
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 2,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 0.5f,
                Shooting = false
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(_entity);
            Assert.AreEqual(1, missile.CurrentShoots);
            Assert.AreEqual(5.0f, missile.ReloadRemaining, 0.01f);
        }

        [Test]
        public void ReloadRemaining_DoesNotDecrease_WhenCurrentShoots_EqualsMaxShoots()
        {
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = false
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(_entity);
            Assert.AreEqual(5.0f, missile.ReloadRemaining);
        }

        [Test]
        public void CurrentShoots_DecreasesBy1_WhenShooting_AndHasAmmo()
        {
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(_entity);
            Assert.AreEqual(0, missile.CurrentShoots);
        }

        [Test]
        public void CurrentShoots_StaysZero_WhenShooting_AndNoAmmo()
        {
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(_entity);
            Assert.AreEqual(0, missile.CurrentShoots);
        }

        [Test]
        public void Shooting_ResetToFalse_AfterUpdate()
        {
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var missile = m_Manager.GetComponentData<MissileData>(_entity);
            Assert.IsFalse(missile.Shooting);
        }

        [Test]
        public void ShootEvent_Created_WhenShooting_AndHasAmmo()
        {
            m_Manager.AddComponentData(_entity, new ShipTag());
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 1,
                ReloadRemaining = 5.0f,
                Shooting = true,
                ShootPosition = new Unity.Mathematics.float2(1f, 2f),
                Direction = new Unity.Mathematics.float2(0f, 1f)
            });

            RunSystem();

            var query = m_Manager.CreateEntityQuery(typeof(MissileShootEvent));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            Assert.AreEqual(1, entities.Length);
            var buffer = m_Manager.GetBuffer<MissileShootEvent>(entities[0]);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(1f, buffer[0].Position.x, 0.01f);
            Assert.AreEqual(2f, buffer[0].Position.y, 0.01f);
            Assert.AreEqual(0f, buffer[0].Direction.x, 0.01f);
            Assert.AreEqual(1f, buffer[0].Direction.y, 0.01f);
            entities.Dispose();
        }

        [Test]
        public void NoShootEvent_WhenShooting_AndNoAmmo()
        {
            m_Manager.SetComponentData(_entity, new MissileData
            {
                MaxShoots = 1,
                ReloadDurationSec = 5.0f,
                CurrentShoots = 0,
                ReloadRemaining = 5.0f,
                Shooting = true
            });

            RunSystem();

            var query = m_Manager.CreateEntityQuery(typeof(MissileShootEvent));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var buffer = m_Manager.GetBuffer<MissileShootEvent>(entities[0]);
            Assert.AreEqual(0, buffer.Length);
            entities.Dispose();
        }
    }
}
