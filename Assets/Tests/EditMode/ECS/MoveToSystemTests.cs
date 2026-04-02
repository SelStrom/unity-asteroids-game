using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class MoveToSystemTests : AsteroidsEcsTestFixture
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
                Speed = 8f,
                Direction = new float2(0f, 1f)
            });
            m_Manager.AddComponentData(_ufoEntity, new MoveToData
            {
                Every = 3f,
                ReadyRemaining = 0.5f
            });
        }

        [Test]
        public void Update_CooldownActive_DirectionUnchanged()
        {
            m_Manager.SetComponentData(_ufoEntity, new MoveToData
            {
                Every = 3f,
                ReadyRemaining = 5f
            });

            var originalMove = m_Manager.GetComponentData<MoveData>(_ufoEntity);
            var originalDirection = originalMove.Direction;

            var system = CreateAndGetSystem<EcsMoveToSystem>();
            var systemHandle = World.GetExistingSystem<EcsMoveToSystem>();

            var state = World.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.World.PushTime(new Unity.Core.TimeData(1f, 1f));
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
            state.World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(_ufoEntity);

            Assert.AreEqual(originalDirection.x, move.Direction.x, 0.001f,
                "Direction.x should not change during cooldown");
            Assert.AreEqual(originalDirection.y, move.Direction.y, 0.001f,
                "Direction.y should not change during cooldown");
        }

        [Test]
        public void Update_CooldownExpired_DirectionUpdated()
        {
            m_Manager.SetComponentData(_ufoEntity, new MoveToData
            {
                Every = 3f,
                ReadyRemaining = 0.5f
            });

            var system = CreateAndGetSystem<EcsMoveToSystem>();
            var systemHandle = World.GetExistingSystem<EcsMoveToSystem>();

            var state = World.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.World.PushTime(new Unity.Core.TimeData(1f, 1f));
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
            state.World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(_ufoEntity);

            Assert.Greater(move.Direction.x, 0f,
                "Direction.x should be positive (ship is to the right)");
        }

        [Test]
        public void Update_CooldownExpired_ReadyRemainingReset()
        {
            m_Manager.SetComponentData(_ufoEntity, new MoveToData
            {
                Every = 3f,
                ReadyRemaining = 0.5f
            });

            var system = CreateAndGetSystem<EcsMoveToSystem>();
            var systemHandle = World.GetExistingSystem<EcsMoveToSystem>();

            var state = World.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.World.PushTime(new Unity.Core.TimeData(1f, 1f));
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
            state.World.PopTime();

            var moveTo = m_Manager.GetComponentData<MoveToData>(_ufoEntity);

            Assert.AreEqual(3f, moveTo.ReadyRemaining, 0.001f,
                "ReadyRemaining should be reset to Every after cooldown expires");
        }
    }
}
