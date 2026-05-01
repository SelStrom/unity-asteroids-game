using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsRocketHomingSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsRocketHomingSystem>();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        private Entity CreateRocketEntity(float2 position, float2 direction, float speed = 10f, float turnRateRad = math.PI, Entity target = default)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketTag());
            m_Manager.AddComponentData(entity, new PlayerRocketTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new RocketHomingData
            {
                TargetEntity = target,
                TurnRateRad = turnRateRad,
                Speed = speed
            });
            return entity;
        }

        [Test]
        public void AcquiresClosestTarget_WhenNoTargetSet()
        {
            var rocket = CreateRocketEntity(new float2(0f, 0f), new float2(1f, 0f));
            var farAsteroid = CreateAsteroidEntity(new float2(100f, 0f), 0f, default, 1);
            var closeAsteroid = CreateAsteroidEntity(new float2(5f, 0f), 0f, default, 1);

            RunSystem(0.001f);

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(closeAsteroid, homing.TargetEntity);
        }

        [Test]
        public void AcquiresLivingTargetOnly_IgnoresDeadTag()
        {
            var rocket = CreateRocketEntity(new float2(0f, 0f), new float2(1f, 0f));
            var dead = CreateAsteroidEntity(new float2(2f, 0f), 0f, default, 1);
            m_Manager.AddComponent<DeadTag>(dead);
            var alive = CreateAsteroidEntity(new float2(10f, 0f), 0f, default, 1);

            RunSystem(0.001f);

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(alive, homing.TargetEntity);
        }

        [Test]
        public void TurnsTowardTarget_WithinTurnRate()
        {
            var target = CreateAsteroidEntity(new float2(0f, 10f), 0f, default, 1);
            var rocket = CreateRocketEntity(new float2(0f, 0f), new float2(1f, 0f), turnRateRad: math.PI / 4f, target: target);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.That(move.Direction.y, Is.GreaterThan(0f), "Должен повернуть вверх");
            Assert.That(math.length(move.Direction), Is.EqualTo(1f).Within(0.001f), "Направление должно быть единичным");
        }

        [Test]
        public void SnapsToTarget_WhenAngleLessThanMaxStep()
        {
            var target = CreateAsteroidEntity(new float2(0f, 10f), 0f, default, 1);
            var rocket = CreateRocketEntity(new float2(0f, 0f), new float2(1f, 0f), turnRateRad: math.PI * 4f, target: target);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.That(move.Direction.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(move.Direction.y, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Reacquires_WhenTargetIsDead()
        {
            var deadTarget = CreateAsteroidEntity(new float2(0f, 5f), 0f, default, 1);
            m_Manager.AddComponent<DeadTag>(deadTarget);
            var liveTarget = CreateAsteroidEntity(new float2(0f, 20f), 0f, default, 1);
            var rocket = CreateRocketEntity(new float2(0f, 0f), new float2(1f, 0f), target: deadTarget);

            RunSystem(0.001f);

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(liveTarget, homing.TargetEntity);
        }

        [Test]
        public void KeepsDirection_WhenNoLivingTargets()
        {
            var rocket = CreateRocketEntity(new float2(0f, 0f), new float2(1f, 0f));

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.That(move.Direction.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(move.Direction.y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void TargetsUfoAndUfoBig()
        {
            var rocket = CreateRocketEntity(new float2(0f, 0f), new float2(1f, 0f));
            var ufo = CreateUfoEntity(new float2(0f, 5f), 0f, default);

            RunSystem(0.001f);

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(ufo, homing.TargetEntity);
        }
    }
}
