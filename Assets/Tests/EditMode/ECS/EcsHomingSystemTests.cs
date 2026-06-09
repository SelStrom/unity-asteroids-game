using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsHomingSystemTests : AsteroidsEcsTestFixture
    {
        private Entity CreateRocketEntity(float2 pos, float speed, float2 dir, float turnSpeed)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = pos,
                Speed = speed,
                Direction = dir
            });
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = dir,
                TargetDirection = 0f
            });
            m_Manager.AddComponentData(entity, new HomingData
            {
                Speed = speed,
                TurnSpeed = turnSpeed
            });
            return entity;
        }

        private Entity CreateEnemyAsteroid(float2 pos)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new AsteroidTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = pos,
                Speed = 0f,
                Direction = new float2(1f, 0f)
            });
            return entity;
        }

        private void RunSystem(float dt)
        {
            var systemHandle = World.GetExistingSystem<EcsHomingSystem>();
            var state = World.Unmanaged.ResolveSystemStateRef(systemHandle);
            state.World.PushTime(new Unity.Core.TimeData(dt, dt));
            World.Unmanaged.GetUnsafeSystemRef<EcsHomingSystem>(systemHandle)
                .OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(systemHandle));
            state.World.PopTime();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            World.CreateSystem<EcsHomingSystem>();
        }

        // (a) Один враг сбоку — ракета поворачивается к нему, но не мгновенно
        [Test]
        public void OneEnemy_RocketTurnsTowardEnemy_ButNotInstantly()
        {
            // Ракета летит вверх, враг — строго справа
            var rocketDir = new float2(0f, 1f); // вверх
            var rocket = CreateRocketEntity(
                pos: float2.zero,
                speed: 5f,
                dir: rocketDir,
                turnSpeed: math.PI // 180 deg/s — медленно при dt=0.01
            );
            CreateEnemyAsteroid(pos: new float2(10f, 0f)); // строго справа

            RunSystem(dt: 0.01f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var newDir = move.Direction;

            // Угол между исходным направлением (вверх) и направлением к цели (вправо) = 90 deg
            // При TurnSpeed=PI (rad/s) и dt=0.01 максимальный шаг = 0.01*PI ~0.0314 rad
            // Значит угол до цели должен уменьшиться (ракета повернулась к врагу)
            // но не стать 0 (большое начальное расхождение)
            var targetDir = new float2(1f, 0f);
            var angleBefore = math.acos(math.clamp(math.dot(rocketDir, targetDir), -1f, 1f));
            var angleAfter = math.acos(math.clamp(math.dot(newDir, targetDir), -1f, 1f));

            Assert.Less(angleAfter, angleBefore - 0.001f,
                "Угол до цели должен уменьшиться после поворота");
            Assert.Greater(angleAfter, 0.001f,
                "При dt=0.01 и TurnSpeed=PI ракета не должна мгновенно выровняться (угол слишком большой)");
        }

        // (b) Из НЕсовпадающего направления при большом dt ракета полностью
        //     доворачивается к цели. RED-gate: на no-op направление осталось бы
        //     исходным (вверх) и не совпало бы с targetDir (вправо).
        [Test]
        public void OneEnemy_FromMisalignedDirection_FullyTurnsToTarget()
        {
            // Ракета летит ВВЕРХ (заведомо не на цель), враг — строго вправо
            var rocket = CreateRocketEntity(
                pos: float2.zero,
                speed: 5f,
                dir: new float2(0f, 1f),
                turnSpeed: math.PI * 4f // быстрый поворот
            );
            CreateEnemyAsteroid(pos: new float2(10f, 0f));

            RunSystem(dt: 1f); // большой dt — должны полностью "прийти" к цели

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var targetDir = math.normalizesafe(new float2(10f, 0f) - float2.zero);

            Assert.AreEqual(targetDir.x, move.Direction.x, 0.001f,
                "Direction.x должен совпасть с targetDir.x после полного доворота");
            Assert.AreEqual(targetDir.y, move.Direction.y, 0.001f,
                "Direction.y должен совпасть с targetDir.y после полного доворота");
        }

        // (c) RotateData.Rotation синхронизируется с фактически повёрнутым
        //     MoveData.Direction. RED-gate: RotateData стартует с ОТЛИЧНОГО от
        //     Direction значения, а Direction стартует не на цель — на no-op
        //     эти два вектора остались бы разными и ассерт упал бы.
        [Test]
        public void AfterUpdate_RotateDataRotation_MatchesTurnedMoveDirection()
        {
            var startDir = new float2(0f, 1f);   // вверх — не на цель
            var rocket = CreateRocketEntity(
                pos: float2.zero,
                speed: 5f,
                dir: startDir,
                turnSpeed: math.PI
            );
            // RotateData намеренно отличается от MoveData.Direction
            m_Manager.SetComponentData(rocket, new RotateData
            {
                Rotation = new float2(-1f, 0f),
                TargetDirection = 0f
            });
            CreateEnemyAsteroid(pos: new float2(5f, 0f)); // вправо

            RunSystem(dt: 0.05f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var rotate = m_Manager.GetComponentData<RotateData>(rocket);

            // Система должна была реально повернуть направление (оно изменилось)...
            Assert.That(math.distance(move.Direction, startDir), Is.GreaterThan(0.001f),
                "MoveData.Direction должен измениться (поворот к цели произошёл)");
            // ...и записать его в RotateData.Rotation (синхронизация)
            Assert.AreEqual(move.Direction.x, rotate.Rotation.x, 0.001f,
                "RotateData.Rotation.x должен совпадать с повёрнутым MoveData.Direction.x");
            Assert.AreEqual(move.Direction.y, rotate.Rotation.y, 0.001f,
                "RotateData.Rotation.y должен совпадать с повёрнутым MoveData.Direction.y");
        }

        // (d) Нет врагов — направление неизменно
        [Test]
        public void NoEnemies_DirectionUnchanged()
        {
            var originalDir = new float2(0f, 1f);
            var rocket = CreateRocketEntity(
                pos: float2.zero,
                speed: 5f,
                dir: originalDir,
                turnSpeed: math.PI * 10f
            );
            // врагов нет

            RunSystem(dt: 0.1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);

            Assert.AreEqual(originalDir.x, move.Direction.x, 0.001f,
                "Direction.x не должен меняться без врагов");
            Assert.AreEqual(originalDir.y, move.Direction.y, 0.001f,
                "Direction.y не должен меняться без врагов");
        }

        // (e) Из двух врагов выбирается ближайший
        [Test]
        public void TwoEnemies_CloserEnemyIsTargeted()
        {
            // Ракета летит вверх, враг1 — вправо (ближе), враг2 — влево (дальше)
            var rocket = CreateRocketEntity(
                pos: float2.zero,
                speed: 5f,
                dir: new float2(0f, 1f),
                turnSpeed: math.PI * 10f // очень быстрый поворот
            );
            var nearEnemy = CreateEnemyAsteroid(pos: new float2(3f, 0f));  // ближний
            var farEnemy = CreateEnemyAsteroid(pos: new float2(-20f, 0f)); // дальний

            RunSystem(dt: 1f); // большой dt — полное выравнивание с целью

            var move = m_Manager.GetComponentData<MoveData>(rocket);

            // При полном выравнивании с ближним врагом (справа) Direction.x > 0
            Assert.Greater(move.Direction.x, 0.9f,
                "Должен выбрать ближнего врага (справа) — Direction.x близко к 1");
        }
    }
}
