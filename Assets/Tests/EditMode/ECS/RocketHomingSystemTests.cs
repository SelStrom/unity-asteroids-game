using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketHomingSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _rocketEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _rocketEntity = CreateRocketEntity(
                position: float2.zero,
                direction: new float2(1f, 0f),
                turnSpeedRadPerSec: math.PI / 2f);
        }

        private void RunSystem(float deltaTime)
        {
            var system = CreateAndGetSystem<EcsRocketHomingSystem>();
            var systemHandle = World.GetExistingSystem<EcsRocketHomingSystem>();

            var state = World.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.World.PushTime(new Unity.Core.TimeData(1f, deltaTime));
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
            state.World.PopTime();
        }

        private float2 GetDirection()
        {
            return m_Manager.GetComponentData<MoveData>(_rocketEntity).Direction;
        }

        private static float AngleDeg(float2 direction)
        {
            return math.degrees(math.atan2(direction.y, direction.x));
        }

        [Test]
        public void TurnsTowardsNearestTarget()
        {
            // Цель строго вверх (90°), ракета летит вправо (0°)
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), age: 3);

            RunSystem(0.5f);

            Assert.Greater(GetDirection().y, 0f,
                "Rocket should turn towards the target above it");
        }

        [Test]
        public void TurnRate_IsClamped()
        {
            // Угол к цели 90°, максимум поворота за 0.5 сек при PI/2 рад/сек — 45°
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), age: 3);

            RunSystem(0.5f);

            Assert.AreEqual(45f, AngleDeg(GetDirection()), 0.01f,
                "Turn must be clamped to TurnSpeedRadPerSec * deltaTime");
        }

        [Test]
        public void WithinTurnRate_PointsExactlyAtTarget()
        {
            // Цель под углом 10° — в пределах максимального поворота 45°
            var targetAngle = math.radians(10f);
            var targetPos = new float2(math.cos(targetAngle), math.sin(targetAngle)) * 10f;
            CreateAsteroidEntity(targetPos, 0f, new float2(1f, 0f), age: 3);

            RunSystem(0.5f);

            Assert.AreEqual(10f, AngleDeg(GetDirection()), 0.01f,
                "Rocket should point exactly at target when within turn rate");
        }

        [Test]
        public void NoTargets_DirectionUnchanged()
        {
            RunSystem(0.5f);

            var direction = GetDirection();
            Assert.AreEqual(1f, direction.x, 0.001f);
            Assert.AreEqual(0f, direction.y, 0.001f);
        }

        [Test]
        public void PicksNearestTarget()
        {
            // Ближняя цель сверху (дистанция 5), дальняя справа (дистанция 10)
            CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), age: 3);
            CreateAsteroidEntity(new float2(10f, 0f), 0f, new float2(1f, 0f), age: 3);

            RunSystem(0.5f);

            Assert.Greater(GetDirection().y, 0f,
                "Rocket should turn towards the nearest target (above), not the far one");
        }

        [Test]
        public void IgnoresDeadTargets()
        {
            // Ближняя цель мертва — ракета остаётся наведённой на живую цель прямо по курсу
            var deadAsteroid = CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), age: 3);
            m_Manager.AddComponent<DeadTag>(deadAsteroid);
            CreateAsteroidEntity(new float2(10f, 0f), 0f, new float2(1f, 0f), age: 3);

            RunSystem(0.5f);

            var direction = GetDirection();
            Assert.AreEqual(1f, direction.x, 0.001f, "Dead target must be ignored");
            Assert.AreEqual(0f, direction.y, 0.001f, "Dead target must be ignored");
        }

        [Test]
        public void UfoIsValidTarget()
        {
            CreateUfoEntity(new float2(0f, 10f), 0f, new float2(1f, 0f));

            RunSystem(0.5f);

            Assert.Greater(GetDirection().y, 0f, "Rocket should home in on small UFO");
        }

        [Test]
        public void UfoBigIsValidTarget()
        {
            CreateUfoBigEntity(new float2(0f, 10f), 0f, new float2(1f, 0f));

            RunSystem(0.5f);

            Assert.Greater(GetDirection().y, 0f, "Rocket should home in on big UFO");
        }

        [Test]
        public void ShipAndBulletsAreNotTargets()
        {
            CreateShipEntity(new float2(0f, 5f));
            CreateBulletEntity(new float2(0f, 3f), 20f, new float2(0f, 1f), 2f, isPlayer: true);

            RunSystem(0.5f);

            var direction = GetDirection();
            Assert.AreEqual(1f, direction.x, 0.001f, "Ship/bullets must not be homing targets");
            Assert.AreEqual(0f, direction.y, 0.001f, "Ship/bullets must not be homing targets");
        }

        [Test]
        public void RotationIsSyncedWithDirection()
        {
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), age: 3);

            RunSystem(0.5f);

            var direction = GetDirection();
            var rotation = m_Manager.GetComponentData<RotateData>(_rocketEntity).Rotation;
            Assert.AreEqual(direction.x, rotation.x, 0.001f,
                "RotateData.Rotation must follow MoveData.Direction");
            Assert.AreEqual(direction.y, rotation.y, 0.001f,
                "RotateData.Rotation must follow MoveData.Direction");
        }

        [Test]
        public void DirectionStaysNormalized()
        {
            CreateAsteroidEntity(new float2(3f, 7f), 0f, new float2(1f, 0f), age: 3);

            RunSystem(0.5f);

            Assert.AreEqual(1f, math.length(GetDirection()), 0.001f,
                "Direction must stay normalized");
        }
    }
}
