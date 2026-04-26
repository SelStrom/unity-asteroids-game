using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    [TestFixture]
    public class RocketComponentTests : AsteroidsEcsTestFixture
    {
        [Test]
        public void RocketLauncherData_Defaults_AreZero()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketLauncherData());

            var data = m_Manager.GetComponentData<RocketLauncherData>(entity);

            Assert.AreEqual(0, data.MaxRockets);
            Assert.AreEqual(0f, data.RespawnDurationSec);
            Assert.AreEqual(0, data.CurrentRockets);
            Assert.AreEqual(0f, data.RespawnRemaining);
            Assert.IsFalse(data.Launching);
            Assert.AreEqual(float2.zero, data.LaunchPosition);
            Assert.AreEqual(float2.zero, data.LaunchDirection);
        }

        [Test]
        public void RocketHomingData_Defaults_AreZero()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketHomingData());

            var data = m_Manager.GetComponentData<RocketHomingData>(entity);

            Assert.AreEqual(0f, data.TurnRateRadPerSec);
            Assert.AreEqual(Entity.Null, data.TargetEntity);
        }

        [Test]
        public void RocketTag_CanBeAdded_AndQueried()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketTag());
            Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
        }

        [Test]
        public void RocketLaunchEventBuffer_CanBeCreated_AndAccepts_Element()
        {
            var entity = CreateRocketLaunchEventSingleton();
            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(entity);

            var shooter = m_Manager.CreateEntity();
            buffer.Add(new RocketLaunchEvent
            {
                ShooterEntity = shooter,
                Position = new float2(1f, 2f),
                Direction = new float2(0f, 1f)
            });

            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(shooter, buffer[0].ShooterEntity);
            Assert.AreEqual(new float2(1f, 2f), buffer[0].Position);
            Assert.AreEqual(new float2(0f, 1f), buffer[0].Direction);
        }

        [Test]
        public void CreateRocketEntity_Has_AllRequiredComponents()
        {
            var target = m_Manager.CreateEntity();
            var entity = CreateRocketEntity(
                position: new float2(5f, 0f),
                speed: 10f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: 1.5f,
                target: target,
                lifeTime: 4f);

            Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RocketHomingData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(new float2(5f, 0f), move.Position);
            Assert.AreEqual(10f, move.Speed);
            Assert.AreEqual(new float2(1f, 0f), move.Direction);

            var homing = m_Manager.GetComponentData<RocketHomingData>(entity);
            Assert.AreEqual(1.5f, homing.TurnRateRadPerSec);
            Assert.AreEqual(target, homing.TargetEntity);

            var life = m_Manager.GetComponentData<LifeTimeData>(entity);
            Assert.AreEqual(4f, life.TimeRemaining);
        }
    }
}
