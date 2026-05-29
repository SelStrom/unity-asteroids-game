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

        private void RunSystem(float deltaTime)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        // ---- Чистая математика поворота (дуга) ----

        [Test]
        public void Steer_WithinTurnRate_ReturnsDesiredImmediately()
        {
            var result = EcsRocketHomingSystem.Steer(
                new float2(1f, 0f), new float2(0f, 1f), math.radians(180f));

            Assert.AreEqual(0f, result.x, 1e-4f);
            Assert.AreEqual(1f, result.y, 1e-4f);
        }

        [Test]
        public void Steer_BeyondTurnRate_RotatesByLimitTowardDesired()
        {
            // 90° между current и desired, лимит 45° -> результат под 45°.
            var result = EcsRocketHomingSystem.Steer(
                new float2(1f, 0f), new float2(0f, 1f), math.radians(45f));

            var expected = new float2(
                math.cos(math.radians(45f)),
                math.sin(math.radians(45f)));

            Assert.AreEqual(expected.x, result.x, 1e-3f);
            Assert.AreEqual(expected.y, result.y, 1e-3f);
            Assert.AreEqual(1f, math.length(result), 1e-3f, "результат должен быть единичным");
        }

        [Test]
        public void Steer_RotatesClockwise_WhenDesiredIsClockwise()
        {
            // desired снизу (0,-1) -> поворот по часовой: y становится отрицательным.
            var result = EcsRocketHomingSystem.Steer(
                new float2(1f, 0f), new float2(0f, -1f), math.radians(30f));

            Assert.Less(result.y, 0f);
            Assert.Greater(result.x, 0f);
        }

        [Test]
        public void Steer_ProducesArc_OverMultipleSteps()
        {
            // Несколько маленьких шагов сходятся к цели, не перепрыгивая её — это и есть дуга.
            var dir = new float2(1f, 0f);
            var desired = new float2(-1f, 0f); // 180°, сложный разворот
            for (var i = 0; i < 100; i++)
            {
                dir = EcsRocketHomingSystem.Steer(dir, desired, math.radians(10f));
            }

            Assert.AreEqual(desired.x, dir.x, 1e-2f);
            Assert.AreEqual(desired.y, dir.y, 1e-2f);
        }

        // ---- Поведение системы наведения ----

        [Test]
        public void Homing_SelectsNearestEnemy_AsTarget()
        {
            var rocket = CreateRocketEntity(
                new float2(0f, 0f), 5f, new float2(1f, 0f), 90f);
            var near = CreateAsteroidEntity(new float2(3f, 0f), 0f, float2.zero, 1);
            var far = CreateAsteroidEntity(new float2(10f, 0f), 0f, float2.zero, 1);

            RunSystem(0.1f);

            var data = m_Manager.GetComponentData<RocketData>(rocket);
            Assert.AreEqual(near, data.Target);
            Assert.AreNotEqual(far, data.Target);
        }

        [Test]
        public void Homing_SteersDirectionTowardTarget()
        {
            // Ракета летит вправо, единственный враг сверху -> direction.y становится > 0.
            var rocket = CreateRocketEntity(
                new float2(0f, 0f), 5f, new float2(1f, 0f), 360f);
            CreateAsteroidEntity(new float2(0f, 5f), 0f, float2.zero, 1);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Greater(move.Direction.y, 0f, "направление должно повернуть к цели");
        }

        [Test]
        public void Homing_UpdatesRotationForVisual()
        {
            var rocket = CreateRocketEntity(
                new float2(0f, 0f), 5f, new float2(1f, 0f), 360f);
            CreateAsteroidEntity(new float2(0f, 5f), 0f, float2.zero, 1);

            RunSystem(0.5f);

            var rotate = m_Manager.GetComponentData<RotateData>(rocket);
            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(move.Direction.x, rotate.Rotation.x, 1e-4f);
            Assert.AreEqual(move.Direction.y, rotate.Rotation.y, 1e-4f);
        }

        [Test]
        public void Homing_ReacquiresTarget_WhenCurrentIsDead()
        {
            var dead = CreateAsteroidEntity(new float2(3f, 0f), 0f, float2.zero, 1);
            m_Manager.AddComponent<DeadTag>(dead);

            var rocket = CreateRocketEntity(
                new float2(0f, 0f), 5f, new float2(1f, 0f), 90f, target: dead);
            var alive = CreateAsteroidEntity(new float2(0f, 4f), 0f, float2.zero, 1);

            RunSystem(0.1f);

            var data = m_Manager.GetComponentData<RocketData>(rocket);
            Assert.AreEqual(alive, data.Target);
        }

        [Test]
        public void Homing_NoEnemies_FliesStraight_AndHasNoTarget()
        {
            var rocket = CreateRocketEntity(
                new float2(0f, 0f), 5f, new float2(1f, 0f), 90f);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, move.Direction.x, 1e-4f);
            Assert.AreEqual(0f, move.Direction.y, 1e-4f);

            var data = m_Manager.GetComponentData<RocketData>(rocket);
            Assert.AreEqual(Entity.Null, data.Target);
        }

        [Test]
        public void Homing_TargetsUfo_NotOnlyAsteroids()
        {
            var rocket = CreateRocketEntity(
                new float2(0f, 0f), 5f, new float2(1f, 0f), 90f);
            var ufo = CreateUfoEntity(new float2(2f, 0f), 0f, float2.zero);

            RunSystem(0.1f);

            var data = m_Manager.GetComponentData<RocketData>(rocket);
            Assert.AreEqual(ufo, data.Target);
        }
    }
}
