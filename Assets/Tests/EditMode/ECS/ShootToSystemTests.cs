using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class ShootToSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _ufoEntity;
        private Entity _shipPosSingleton;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _shipPosSingleton = CreateShipPositionSingleton(
                position: new float2(10f, 0f),
                speed: 5f,
                direction: new float2(1f, 0f)
            );

            _ufoEntity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(_ufoEntity, new MoveData
            {
                Position = new float2(0f, 0f),
                Speed = 3f,
                Direction = new float2(1f, 0f)
            });
            m_Manager.AddComponentData(_ufoEntity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 1f,
                CurrentShoots = 3,
                ReloadRemaining = 0f,
                Shooting = false,
                Direction = float2.zero,
                ShootPosition = float2.zero
            });
            m_Manager.AddComponentData(_ufoEntity, new ShootToData
            {
                Every = 1f,
                ReadyRemaining = 0f
            });
        }

        [Test]
        public void Update_HasAmmo_SetsShootingAndDirection()
        {
            var system = CreateAndGetSystem<EcsShootToSystem>();
            var systemHandle = World.GetExistingSystem<EcsShootToSystem>();
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));

            var gun = m_Manager.GetComponentData<GunData>(_ufoEntity);

            Assert.IsTrue(gun.Shooting, "Shooting should be true when has ammo");
            Assert.AreNotEqual(float2.zero, gun.Direction, "Direction should be calculated");
            Assert.Greater(gun.Direction.x, 0f, "Direction.x should be positive (ship is to the right)");
        }

        [Test]
        public void Update_NoAmmo_DoesNotShoot()
        {
            m_Manager.SetComponentData(_ufoEntity, new GunData
            {
                MaxShoots = 5,
                ReloadDurationSec = 1f,
                CurrentShoots = 0,
                ReloadRemaining = 0f,
                Shooting = false,
                Direction = float2.zero,
                ShootPosition = float2.zero
            });

            var system = CreateAndGetSystem<EcsShootToSystem>();
            var systemHandle = World.GetExistingSystem<EcsShootToSystem>();
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));

            var gun = m_Manager.GetComponentData<GunData>(_ufoEntity);

            Assert.IsFalse(gun.Shooting, "Shooting should remain false when no ammo");
        }

        [Test]
        public void Update_SetsShootPositionToUfoPosition()
        {
            var system = CreateAndGetSystem<EcsShootToSystem>();
            var systemHandle = World.GetExistingSystem<EcsShootToSystem>();
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));

            var gun = m_Manager.GetComponentData<GunData>(_ufoEntity);
            var move = m_Manager.GetComponentData<MoveData>(_ufoEntity);

            Assert.AreEqual(move.Position.x, gun.ShootPosition.x, 0.001f,
                "ShootPosition.x should match ufo Position.x");
            Assert.AreEqual(move.Position.y, gun.ShootPosition.y, 0.001f,
                "ShootPosition.y should match ufo Position.y");
        }
    }
}
