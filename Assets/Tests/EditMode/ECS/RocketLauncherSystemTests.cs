using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketLauncherSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _shipEntity;
        private Entity _eventSingleton;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _eventSingleton = m_Manager.CreateEntity();
            m_Manager.AddBuffer<RocketLaunchEvent>(_eventSingleton);

            _shipEntity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(_shipEntity, new ShipTag());
            m_Manager.AddComponentData(_shipEntity, new RocketAmmoData
            {
                MaxRockets = 1,
                RespawnDurationSec = 10f,
                CurrentRockets = 1,
                RespawnRemaining = 10f
            });
        }

        private void RunSystem(float deltaTime)
        {
            var system = CreateAndGetSystem<EcsRocketLauncherSystem>();
            var systemHandle = World.GetExistingSystem<EcsRocketLauncherSystem>();

            var state = World.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.World.PushTime(new Unity.Core.TimeData(1f, deltaTime));
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
            state.World.PopTime();
        }

        private RocketAmmoData GetAmmo()
        {
            return m_Manager.GetComponentData<RocketAmmoData>(_shipEntity);
        }

        private void SetAmmo(RocketAmmoData ammo)
        {
            m_Manager.SetComponentData(_shipEntity, ammo);
        }

        [Test]
        public void Launch_WithAmmo_EmitsEventAndDecrementsAmmo()
        {
            var ammo = GetAmmo();
            ammo.Launching = true;
            ammo.LaunchPosition = new float2(1f, 2f);
            ammo.Direction = new float2(0f, 1f);
            SetAmmo(ammo);

            RunSystem(0.1f);

            var events = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(1, events.Length, "Exactly one launch event should be emitted");
            Assert.AreEqual(0, GetAmmo().CurrentRockets, "CurrentRockets should be decremented");
        }

        [Test]
        public void Launch_EventCarriesPositionAndDirection()
        {
            var ammo = GetAmmo();
            ammo.Launching = true;
            ammo.LaunchPosition = new float2(3f, -4f);
            ammo.Direction = new float2(0f, -1f);
            SetAmmo(ammo);

            RunSystem(0.1f);

            var events = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(3f, events[0].Position.x, 0.001f);
            Assert.AreEqual(-4f, events[0].Position.y, 0.001f);
            Assert.AreEqual(0f, events[0].Direction.x, 0.001f);
            Assert.AreEqual(-1f, events[0].Direction.y, 0.001f);
        }

        [Test]
        public void Launch_WithoutAmmo_NoEventEmitted()
        {
            var ammo = GetAmmo();
            ammo.CurrentRockets = 0;
            ammo.Launching = true;
            SetAmmo(ammo);

            RunSystem(0.1f);

            var events = m_Manager.GetBuffer<RocketLaunchEvent>(_eventSingleton);
            Assert.AreEqual(0, events.Length, "No event should be emitted without ammo");
        }

        [Test]
        public void Launch_FlagIsResetAfterUpdate()
        {
            var ammo = GetAmmo();
            ammo.Launching = true;
            SetAmmo(ammo);

            RunSystem(0.1f);

            Assert.IsFalse(GetAmmo().Launching, "Launching flag should be reset after update");
        }

        [Test]
        public void Launch_FlagIsResetEvenWithoutAmmo()
        {
            var ammo = GetAmmo();
            ammo.CurrentRockets = 0;
            ammo.Launching = true;
            SetAmmo(ammo);

            RunSystem(0.1f);

            Assert.IsFalse(GetAmmo().Launching, "Launching flag should be reset even without ammo");
        }

        [Test]
        public void Respawn_FullAmmo_TimerDoesNotTick()
        {
            RunSystem(1f);

            Assert.AreEqual(10f, GetAmmo().RespawnRemaining, 0.001f,
                "RespawnRemaining should not tick while ammo is full");
        }

        [Test]
        public void Respawn_BelowMax_TimerTicks()
        {
            var ammo = GetAmmo();
            ammo.CurrentRockets = 0;
            SetAmmo(ammo);

            RunSystem(1f);

            Assert.AreEqual(9f, GetAmmo().RespawnRemaining, 0.001f,
                "RespawnRemaining should tick down while below max");
        }

        [Test]
        public void Respawn_TimerExpired_RestoresOneRocketAndResetsTimer()
        {
            var ammo = GetAmmo();
            ammo.CurrentRockets = 0;
            ammo.RespawnRemaining = 0.5f;
            SetAmmo(ammo);

            RunSystem(1f);

            ammo = GetAmmo();
            Assert.AreEqual(1, ammo.CurrentRockets, "One rocket should be restored");
            Assert.AreEqual(10f, ammo.RespawnRemaining, 0.001f,
                "RespawnRemaining should be reset to RespawnDurationSec");
        }

        [Test]
        public void Respawn_MultipleMissing_RestoresOnlyOnePerPeriod()
        {
            var ammo = GetAmmo();
            ammo.MaxRockets = 2;
            ammo.CurrentRockets = 0;
            ammo.RespawnRemaining = 0.5f;
            SetAmmo(ammo);

            RunSystem(1f);

            Assert.AreEqual(1, GetAmmo().CurrentRockets,
                "Only one rocket should be restored per respawn period");
        }
    }
}
