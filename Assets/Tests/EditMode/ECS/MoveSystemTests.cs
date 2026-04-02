using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class MoveSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _moveSystemHandle;
        private SystemHandle _shipPositionUpdateHandle;

        public override void SetUp()
        {
            base.SetUp();
            _moveSystemHandle = World.CreateSystem<EcsMoveSystem>();
            _shipPositionUpdateHandle = World.CreateSystem<EcsShipPositionUpdateSystem>();
            CreateGameAreaSingleton(new float2(20f, 15f));
            CreateShipPositionSingleton(float2.zero, 0f, new float2(1f, 0f));
        }

        private Entity CreateMoveEntity(float2 position, float speed, float2 direction)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            return entity;
        }

        [Test]
        public void MoveSystem_UpdatesPosition()
        {
            var entity = CreateMoveEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f)
            );

            World.PushTime(new Unity.Core.TimeData(0.1, 0.1));
            _moveSystemHandle.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(1f, move.Position.x, 0.001f);
            Assert.AreEqual(0f, move.Position.y, 0.001f);
        }

        [Test]
        public void MoveSystem_WrapsRight_ToLeft()
        {
            // GameArea = 20x15, half = 10
            // Position 9.5 + speed 10 * dt 0.1 = 10.5 > 10 -> wrap: -20 + 10.5 = -9.5
            var entity = CreateMoveEntity(
                position: new float2(9.5f, 0f),
                speed: 10f,
                direction: new float2(1f, 0f)
            );

            World.PushTime(new Unity.Core.TimeData(0.1, 0.1));
            _moveSystemHandle.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(-9.5f, move.Position.x, 0.001f);
        }

        [Test]
        public void MoveSystem_WrapsLeft_ToRight()
        {
            // Position -9.5 + speed 10 * dt 0.1 in -x = -10.5 < -10 -> wrap: 20 - 10.5 = 9.5
            var entity = CreateMoveEntity(
                position: new float2(-9.5f, 0f),
                speed: 10f,
                direction: new float2(-1f, 0f)
            );

            World.PushTime(new Unity.Core.TimeData(0.1, 0.1));
            _moveSystemHandle.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(9.5f, move.Position.x, 0.001f);
        }

        [Test]
        public void ShipPositionUpdateSystem_UpdatesSingleton()
        {
            var shipEntity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(shipEntity, new ShipTag());
            m_Manager.AddComponentData(shipEntity, new MoveData
            {
                Position = new float2(5f, 3f),
                Speed = 7f,
                Direction = new float2(0f, 1f)
            });

            World.PushTime(new Unity.Core.TimeData(0.1, 0.1));
            _moveSystemHandle.Update(World.Unmanaged);
            _shipPositionUpdateHandle.Update(World.Unmanaged);
            World.PopTime();

            var singleton = m_Manager.CreateEntityQuery(typeof(ShipPositionData))
                .GetSingleton<ShipPositionData>();

            // Position should be updated after move: 5 + 0*7*0.1 = 5 (direction is (0,1))
            // Actually x stays, y moves: 3 + 1*7*0.1 = 3.7
            Assert.AreEqual(5f, singleton.Position.x, 0.1f);
            Assert.AreEqual(3.7f, singleton.Position.y, 0.1f);
            Assert.AreEqual(7f, singleton.Speed, 0.001f);
        }
    }
}
