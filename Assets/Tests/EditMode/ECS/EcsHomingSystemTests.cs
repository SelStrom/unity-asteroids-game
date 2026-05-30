using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsHomingSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsHomingSystem>();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void SteersTowardEnemy_WhenTurnRateAllowsFullTurn()
        {
            // Ракета летит вправо, враг строго сверху (поворот на 90°), turnRate с запасом.
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f),
                turnRateDegPerSec: 180f);
            CreateAsteroidEntity(new float2(0f, 5f), 1f, new float2(0f, 1f), age: 3);

            RunSystem(deltaTime: 1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Greater(move.Direction.y, 0.9f,
                "Direction should turn toward the enemy located above");
            Assert.Less(math.abs(move.Direction.x), 0.2f);
        }

        [Test]
        public void Arc_TurnRateLimitsRotationPerStep()
        {
            // turnRate=90°/сек, dt=0.1 → не более ~9° за кадр. Враг сверху (нужно 90°).
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f),
                turnRateDegPerSec: 90f);
            CreateAsteroidEntity(new float2(0f, 5f), 1f, new float2(0f, 1f), age: 3);

            RunSystem(deltaTime: 0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            // Поворот ограничен ~9°: x ещё доминирует, y маленький положительный — это и есть дуга.
            Assert.Greater(move.Direction.x, 0.9f, "Rotation must be clamped (arc), not snap");
            Assert.Greater(move.Direction.y, 0.05f, "Should start turning toward target");
            Assert.Less(move.Direction.y, 0.3f, "Turn per frame must be limited");
        }

        [Test]
        public void PicksNearestEnemy_AmongSeveral()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f),
                turnRateDegPerSec: 180f);
            // Ближний — снизу (дист 2), дальний — сверху (дист 10).
            CreateAsteroidEntity(new float2(0f, -2f), 1f, new float2(0f, -1f), age: 3);
            CreateUfoEntity(new float2(0f, 10f), 1f, new float2(0f, 1f));

            RunSystem(deltaTime: 1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Less(move.Direction.y, -0.9f,
                "Should steer toward the NEAREST enemy (below)");
        }

        [Test]
        public void KeepsDirection_WhenNoEnemies()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f),
                turnRateDegPerSec: 180f);

            RunSystem(deltaTime: 1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, move.Direction.x, 0.0001f);
            Assert.AreEqual(0f, move.Direction.y, 0.0001f);
        }

        [Test]
        public void UpdatesRotation_ToMatchTravelDirection()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero, speed: 10f, direction: new float2(1f, 0f),
                turnRateDegPerSec: 180f);
            CreateAsteroidEntity(new float2(0f, 5f), 1f, new float2(0f, 1f), age: 3);

            RunSystem(deltaTime: 1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var rotate = m_Manager.GetComponentData<RotateData>(rocket);
            Assert.AreEqual(move.Direction.x, rotate.Rotation.x, 0.0001f);
            Assert.AreEqual(move.Direction.y, rotate.Rotation.y, 0.0001f);
        }
    }
}
