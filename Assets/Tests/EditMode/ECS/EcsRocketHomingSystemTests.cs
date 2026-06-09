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

        private static void AssertDirection(float2 expected, float2 actual, float tolerance = 1e-3f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance, "Direction.x");
            Assert.AreEqual(expected.y, actual.y, tolerance, "Direction.y");
        }

        [Test]
        public void Homing_AcquiresNearestTarget()
        {
            var nearAsteroid = CreateAsteroidEntity(new float2(0f, 2f), 0f, new float2(1f, 0f), 1);
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 1);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));

            RunSystem();

            var rocketData = m_Manager.GetComponentData<RocketData>(rocket);
            Assert.AreEqual(nearAsteroid, rocketData.TargetEntity);
        }

        [Test]
        public void Homing_TurnsTowardTarget_ClampedByTurnSpeed()
        {
            // Цель строго вверх, ракета смотрит вправо: до цели 90°,
            // при 90°/сек и dt=0.5 доворот ровно на 45°
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 1);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f),
                turnSpeedDegPerSec: 90f);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var expected = new float2(math.cos(math.radians(45f)), math.sin(math.radians(45f)));
            AssertDirection(expected, move.Direction);
        }

        [Test]
        public void Homing_DoesNotOverturn_WhenTargetWithinTurnRate()
        {
            // Цель под 90°, поворот 180°/сек за 1 сек хватает с запасом —
            // ракета доворачивает ровно на цель, без перелёта
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 1);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f),
                turnSpeedDegPerSec: 180f);

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            AssertDirection(new float2(0f, 1f), move.Direction);
        }

        [Test]
        public void Homing_Retargets_WhenTargetIsDead()
        {
            var firstTarget = CreateAsteroidEntity(new float2(0f, 2f), 0f, new float2(1f, 0f), 1);
            var secondTarget = CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), 1);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));

            RunSystem();
            Assert.AreEqual(firstTarget,
                m_Manager.GetComponentData<RocketData>(rocket).TargetEntity);

            m_Manager.AddComponent<DeadTag>(firstTarget);
            RunSystem();

            Assert.AreEqual(secondTarget,
                m_Manager.GetComponentData<RocketData>(rocket).TargetEntity);
        }

        [Test]
        public void Homing_Retargets_WhenTargetDestroyed()
        {
            var firstTarget = CreateAsteroidEntity(new float2(0f, 2f), 0f, new float2(1f, 0f), 1);
            var secondTarget = CreateUfoEntity(new float2(0f, 5f), 0f, new float2(1f, 0f));
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));

            RunSystem();
            m_Manager.DestroyEntity(firstTarget);
            RunSystem();

            Assert.AreEqual(secondTarget,
                m_Manager.GetComponentData<RocketData>(rocket).TargetEntity);
        }

        [Test]
        public void Homing_FliesStraight_WhenNoTargets()
        {
            var direction = math.normalize(new float2(1f, 1f));
            var rocket = CreateRocketEntity(float2.zero, 5f, direction);

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            AssertDirection(direction, move.Direction);
            Assert.AreEqual(Entity.Null,
                m_Manager.GetComponentData<RocketData>(rocket).TargetEntity);
        }

        [Test]
        public void Homing_UpdatesRotation_ToMatchDirection()
        {
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 1);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f),
                turnSpeedDegPerSec: 90f);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var rotate = m_Manager.GetComponentData<RotateData>(rocket);
            AssertDirection(move.Direction, rotate.Rotation);
        }

        [Test]
        public void Homing_TargetsUfo_WhenItIsNearest()
        {
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), 1);
            var ufo = CreateUfoBigEntity(new float2(0f, 3f), 0f, new float2(1f, 0f));
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));

            RunSystem();

            Assert.AreEqual(ufo,
                m_Manager.GetComponentData<RocketData>(rocket).TargetEntity);
        }

        [Test]
        public void Homing_IgnoresDeadEntities_WhenAcquiringTarget()
        {
            var deadAsteroid = CreateAsteroidEntity(new float2(0f, 1f), 0f, new float2(1f, 0f), 1);
            m_Manager.AddComponent<DeadTag>(deadAsteroid);
            var aliveAsteroid = CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), 1);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f));

            RunSystem();

            Assert.AreEqual(aliveAsteroid,
                m_Manager.GetComponentData<RocketData>(rocket).TargetEntity);
        }
    }
}
