using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketGuidanceSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var system = World.CreateSystemManaged<EcsRocketGuidanceSystem>();
            _systemHandle = system.SystemHandle;
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void NoEnemies_DirectionUnchanged()
        {
            // Ракета летит вправо (1,0), врагов нет -- Direction не меняется
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, move.Direction.x, 0.001f);
            Assert.AreEqual(0f, move.Direction.y, 0.001f);
        }

        [Test]
        public void SingleEnemy_TurnsTowardsEnemy()
        {
            // Ракета (0,0) летит вправо, враг (0,10) сверху -- Direction.y > 0 после 0.1 сек
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);

            CreateAsteroidEntity(
                position: new float2(0f, 10f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.Greater(move.Direction.y, 0f,
                "Ракета должна повернуть к врагу сверху, Direction.y > 0");
        }

        [Test]
        public void TurnRate_LimitsRotationPerFrame()
        {
            // Ракета вправо (1,0), враг точно сверху, turnRate=90 deg/sec, dt=1.0
            // Максимум 90 градусов -- Direction должен быть ~(0,1)
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 90f);

            CreateAsteroidEntity(
                position: new float2(0f, 10f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, move.Direction.x, 0.01f);
            Assert.AreEqual(1f, move.Direction.y, 0.01f);
        }

        [Test]
        public void MultipleEnemies_TargetsClosest()
        {
            // Ракета (0,0), враг A (0,5) ближе, враг B (0,20) дальше -- Target == entityA
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);

            var enemyA = CreateAsteroidEntity(
                position: new float2(0f, 5f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            CreateAsteroidEntity(
                position: new float2(0f, 20f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            RunSystem(0.1f);

            var target = m_Manager.GetComponentData<RocketTargetData>(rocket);
            Assert.AreEqual(enemyA, target.Target,
                "Ракета должна выбрать ближайшего врага");
        }

        [Test]
        public void TargetWithDeadTag_Retargets()
        {
            // Ракета с Target=asteroidA, asteroidA получает DeadTag -- Target переключается на asteroidB
            var enemyA = CreateAsteroidEntity(
                position: new float2(0f, 5f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            var enemyB = CreateAsteroidEntity(
                position: new float2(10f, 0f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);

            // Установить Target вручную на enemyA
            m_Manager.SetComponentData(rocket, new RocketTargetData
            {
                Target = enemyA,
                TurnRateDegPerSec = 180f
            });

            // Пометить enemyA как мёртвого
            m_Manager.AddComponentData(enemyA, new DeadTag());

            RunSystem(0.1f);

            var target = m_Manager.GetComponentData<RocketTargetData>(rocket);
            Assert.AreEqual(enemyB, target.Target,
                "При DeadTag на цели ракета должна переключиться на другого врага");
        }

        [Test]
        public void AllEnemiesDead_FliesStraight()
        {
            // Все враги с DeadTag -- Direction не меняется
            var enemy = CreateAsteroidEntity(
                position: new float2(0f, 10f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);
            m_Manager.AddComponentData(enemy, new DeadTag());

            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, move.Direction.x, 0.001f);
            Assert.AreEqual(0f, move.Direction.y, 0.001f);
        }

        [Test]
        public void RocketWithDeadTag_NotProcessed()
        {
            // Ракета с DeadTag -- Direction не меняется (система пропускает мёртвые ракеты)
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);
            m_Manager.AddComponentData(rocket, new DeadTag());

            CreateAsteroidEntity(
                position: new float2(0f, 10f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, move.Direction.x, 0.001f);
            Assert.AreEqual(0f, move.Direction.y, 0.001f);
        }

        [Test]
        public void AlreadyFacingTarget_DirectionUnchanged()
        {
            // Ракета летит точно к цели -- Direction не меняется существенно
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(0f, 1f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);

            CreateAsteroidEntity(
                position: new float2(0f, 10f),
                speed: 0f,
                direction: new float2(1f, 0f),
                age: 3);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, move.Direction.x, 0.01f);
            Assert.AreEqual(1f, move.Direction.y, 0.01f);
        }

        [Test]
        public void TargetEntityDestroyed_Retargets()
        {
            // Target entity уничтожена через DestroyEntity -- Target переключается
            var enemyA = CreateAsteroidEntity(
                position: new float2(0f, 5f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            var enemyB = CreateAsteroidEntity(
                position: new float2(10f, 0f),
                speed: 1f,
                direction: new float2(1f, 0f),
                age: 3);

            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                lifeTime: 5f,
                turnRateDegPerSec: 180f);

            // Установить Target на enemyA
            m_Manager.SetComponentData(rocket, new RocketTargetData
            {
                Target = enemyA,
                TurnRateDegPerSec = 180f
            });

            // Уничтожить enemyA
            m_Manager.DestroyEntity(enemyA);

            RunSystem(0.1f);

            var target = m_Manager.GetComponentData<RocketTargetData>(rocket);
            Assert.AreEqual(enemyB, target.Target,
                "При уничтожении цели ракета должна переключиться на другого врага");
        }
    }
}
