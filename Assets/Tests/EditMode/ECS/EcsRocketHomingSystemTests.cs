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

        private Entity CreateRocketEntity(float2 position, float2 direction,
            float turnRateDegPerSec = 90f, Entity target = default)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = 5f,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new RocketHomingData
            {
                TargetEntity = target,
                TurnRateDegPerSec = turnRateDegPerSec
            });
            return entity;
        }

        private static void AssertDirection(float2 expected, float2 actual, float tolerance = 1e-3f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance, "Direction.x");
            Assert.AreEqual(expected.y, actual.y, tolerance, "Direction.y");
        }

        [Test]
        public void AcquiresNearestTarget()
        {
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f));
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 3);
            var nearUfo = CreateUfoEntity(new float2(0f, -5f), 0f, new float2(1f, 0f));

            RunSystem();

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(nearUfo, homing.TargetEntity);
        }

        [Test]
        public void TurnsTowardTarget_LimitedByTurnRate()
        {
            // Цель строго сверху (90°), скорость поворота 90°/сек, dt = 0.5 → поворот на 45°
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f), 90f);
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 3);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var expected = new float2(math.cos(math.radians(45f)), math.sin(math.radians(45f)));
            AssertDirection(expected, move.Direction);
        }

        [Test]
        public void TurnsClockwise_WhenTargetBelow()
        {
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f), 90f);
            CreateAsteroidEntity(new float2(0f, -10f), 0f, new float2(1f, 0f), 3);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var expected = new float2(math.cos(math.radians(-45f)), math.sin(math.radians(-45f)));
            AssertDirection(expected, move.Direction);
        }

        [Test]
        public void PointsExactlyAtTarget_WhenTurnWithinRate()
        {
            // Цель сверху (90°), большой dt — направление не должно «перекрутиться»
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f), 90f);
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 3);

            RunSystem(10f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            AssertDirection(new float2(0f, 1f), move.Direction);
        }

        [Test]
        public void KeepsDirection_WhenNoTargets()
        {
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f));

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            AssertDirection(new float2(1f, 0f), move.Direction);
        }

        [Test]
        public void Retargets_WhenTargetIsDead()
        {
            var deadTarget = CreateAsteroidEntity(new float2(0f, 1f), 0f, new float2(1f, 0f), 3);
            m_Manager.AddComponent<DeadTag>(deadTarget);
            var aliveTarget = CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), 3);
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f), 90f, deadTarget);

            RunSystem();

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(aliveTarget, homing.TargetEntity);
        }

        [Test]
        public void Retargets_WhenTargetDestroyed()
        {
            var target = CreateAsteroidEntity(new float2(0f, 1f), 0f, new float2(1f, 0f), 3);
            var other = CreateUfoBigEntity(new float2(3f, 0f), 0f, new float2(1f, 0f));
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f), 90f, target);
            m_Manager.DestroyEntity(target);

            RunSystem();

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(other, homing.TargetEntity);
        }

        [Test]
        public void KeepsExistingTarget_EvenIfAnotherIsCloser()
        {
            // Выбранная цель сохраняется — ракета не «дёргается» между целями
            var target = CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 3);
            CreateAsteroidEntity(new float2(0f, 1f), 0f, new float2(1f, 0f), 3);
            var rocket = CreateRocketEntity(float2.zero, new float2(1f, 0f), 90f, target);

            RunSystem();

            var homing = m_Manager.GetComponentData<RocketHomingData>(rocket);
            Assert.AreEqual(target, homing.TargetEntity);
        }
    }
}
