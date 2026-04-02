using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class ThrustSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsThrustSystem>();
            World.CreateSystem<EcsRotateSystem>();
        }

        private Entity CreateThrustEntity(
            float speed, float2 direction, float2 rotation,
            float unitsPerSecond, float maxSpeed, bool isActive)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = float2.zero,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = rotation,
                TargetDirection = 0f
            });
            m_Manager.AddComponentData(entity, new ThrustData
            {
                UnitsPerSecond = unitsPerSecond,
                MaxSpeed = maxSpeed,
                IsActive = isActive
            });
            return entity;
        }

        [Test]
        public void ThrustSystem_Active_IncreasesSpeed()
        {
            var entity = CreateThrustEntity(
                speed: 0f,
                direction: new float2(1f, 0f),
                rotation: new float2(1f, 0f),
                unitsPerSecond: 10f,
                maxSpeed: 20f,
                isActive: true
            );

            World.PushTime(new Unity.Core.TimeData(0.1, 0.1));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.Greater(move.Speed, 0f);
        }

        [Test]
        public void ThrustSystem_Active_SpeedDoesNotExceedMaxSpeed()
        {
            var entity = CreateThrustEntity(
                speed: 19f,
                direction: new float2(1f, 0f),
                rotation: new float2(1f, 0f),
                unitsPerSecond: 100f,
                maxSpeed: 20f,
                isActive: true
            );

            World.PushTime(new Unity.Core.TimeData(1.0, 1.0));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.LessOrEqual(move.Speed, 20f);
        }

        [Test]
        public void ThrustSystem_Inactive_DecreasesSpeed()
        {
            var entity = CreateThrustEntity(
                speed: 10f,
                direction: new float2(1f, 0f),
                rotation: new float2(1f, 0f),
                unitsPerSecond: 10f,
                maxSpeed: 20f,
                isActive: false
            );

            World.PushTime(new Unity.Core.TimeData(1.0, 1.0));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(5f, move.Speed, 0.001f);
        }

        [Test]
        public void ThrustSystem_Inactive_SpeedDoesNotGoBelowZero()
        {
            var entity = CreateThrustEntity(
                speed: 1f,
                direction: new float2(1f, 0f),
                rotation: new float2(1f, 0f),
                unitsPerSecond: 100f,
                maxSpeed: 20f,
                isActive: false
            );

            World.PushTime(new Unity.Core.TimeData(1.0, 1.0));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(ThrustData.MinSpeed, move.Speed, 0.001f);
        }
    }
}
