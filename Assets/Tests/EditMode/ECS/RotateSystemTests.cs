using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RotateSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsRotateSystem>();
        }

        private Entity CreateRotateEntity(float2 rotation, float targetDirection)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = rotation,
                TargetDirection = targetDirection
            });
            return entity;
        }

        [Test]
        public void RotateSystem_CounterClockwise_RotatesBy90Degrees()
        {
            var entity = CreateRotateEntity(new float2(1f, 0f), 1f);

            World.PushTime(new Unity.Core.TimeData(1.0, 1.0));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();

            var rotate = m_Manager.GetComponentData<RotateData>(entity);
            Assert.AreEqual(0f, rotate.Rotation.x, 0.001f);
            Assert.AreEqual(1f, rotate.Rotation.y, 0.001f);
        }

        [Test]
        public void RotateSystem_Clockwise_RotatesByMinus90Degrees()
        {
            var entity = CreateRotateEntity(new float2(1f, 0f), -1f);

            World.PushTime(new Unity.Core.TimeData(1.0, 1.0));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();

            var rotate = m_Manager.GetComponentData<RotateData>(entity);
            Assert.AreEqual(0f, rotate.Rotation.x, 0.001f);
            Assert.AreEqual(-1f, rotate.Rotation.y, 0.001f);
        }

        [Test]
        public void RotateSystem_ZeroDirection_DoesNotRotate()
        {
            var entity = CreateRotateEntity(new float2(1f, 0f), 0f);

            World.PushTime(new Unity.Core.TimeData(1.0, 1.0));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();

            var rotate = m_Manager.GetComponentData<RotateData>(entity);
            Assert.AreEqual(1f, rotate.Rotation.x, 0.001f);
            Assert.AreEqual(0f, rotate.Rotation.y, 0.001f);
        }
    }
}
