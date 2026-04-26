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

        private Entity CreateMissileEntity(
            float2 position,
            float2 direction,
            float speed = 5f,
            Entity target = default,
            float turnRateRadPerSec = math.PI,
            float acquisitionRange = 100f)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Direction = direction,
                Speed = speed
            });
            m_Manager.AddComponentData(entity, new HomingData
            {
                TargetEntity = target,
                TurnRateRadPerSec = turnRateRadPerSec,
                TargetAcquisitionRange = acquisitionRange
            });
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = direction,
                TargetDirection = 0f
            });
            return entity;
        }

        [Test]
        public void AcquiresNearestEnemy_WhenTargetEntityIsNull()
        {
            var nearby = CreateAsteroidEntity(new float2(2f, 0f), 0f, default, 3);
            var faraway = CreateAsteroidEntity(new float2(50f, 0f), 0f, default, 3);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: Entity.Null);

            RunSystem();

            var homing = m_Manager.GetComponentData<HomingData>(missile);
            Assert.AreEqual(nearby, homing.TargetEntity, "Должна выбрать ближайшую цель");
            Assert.AreNotEqual(faraway, homing.TargetEntity);
        }

        [Test]
        public void AcquiresUfo_AsValidTarget()
        {
            var ufo = CreateUfoEntity(new float2(3f, 0f), 0f, default);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: Entity.Null);

            RunSystem();

            var homing = m_Manager.GetComponentData<HomingData>(missile);
            Assert.AreEqual(ufo, homing.TargetEntity);
        }

        [Test]
        public void AcquiresUfoBig_AsValidTarget()
        {
            var ufoBig = CreateUfoBigEntity(new float2(3f, 0f), 0f, default);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: Entity.Null);

            RunSystem();

            var homing = m_Manager.GetComponentData<HomingData>(missile);
            Assert.AreEqual(ufoBig, homing.TargetEntity);
        }

        [Test]
        public void DoesNotAcquireTarget_OutsideAcquisitionRange()
        {
            CreateAsteroidEntity(new float2(50f, 0f), 0f, default, 3);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: Entity.Null,
                acquisitionRange: 10f);

            RunSystem();

            var homing = m_Manager.GetComponentData<HomingData>(missile);
            Assert.AreEqual(Entity.Null, homing.TargetEntity);
        }

        [Test]
        public void KeepsDirection_WhenTargetIsExactlyAhead()
        {
            var target = CreateAsteroidEntity(new float2(10f, 0f), 0f, default, 3);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: target);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            Assert.AreEqual(1f, move.Direction.x, 1e-3f);
            Assert.AreEqual(0f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void TurnsDirection_TowardTarget_WithinTurnRate()
        {
            var target = CreateAsteroidEntity(new float2(0f, 10f), 0f, default, 3);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: target,
                turnRateRadPerSec: math.PI / 2f); // 90 deg/sec

            RunSystem(0.5f); // 45 deg max turn

            var move = m_Manager.GetComponentData<MoveData>(missile);
            var angle = math.atan2(move.Direction.y, move.Direction.x);
            // ожидаем ровно +45° (PI/4), потому что цель в 90° от курса, а лимит — 45°
            Assert.AreEqual(math.PI / 4f, angle, 1e-3f);
            Assert.AreEqual(1f, math.length(move.Direction), 1e-3f);
        }

        [Test]
        public void SnapsToTarget_WhenAngleSmallerThanTurnRate()
        {
            // цель чуть выше курса (на ~5.7°), turn rate 1 rad/sec, dt = 1 → лимит 1 rad ≈ 57°
            // следовательно за 1 кадр направление должно полностью совпасть с направлением на цель
            var target = CreateAsteroidEntity(new float2(10f, 1f), 0f, default, 3);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: target,
                turnRateRadPerSec: 1f);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            var expected = math.normalize(new float2(10f, 1f));
            Assert.AreEqual(expected.x, move.Direction.x, 1e-3f);
            Assert.AreEqual(expected.y, move.Direction.y, 1e-3f);
        }

        [Test]
        public void KeepsDirection_WhenNoTargetAvailable()
        {
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(0f, 1f),
                target: Entity.Null,
                acquisitionRange: 1f);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            Assert.AreEqual(0f, move.Direction.x, 1e-3f);
            Assert.AreEqual(1f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void ClearsTargetEntity_WhenTargetIsDead()
        {
            var target = CreateAsteroidEntity(new float2(5f, 0f), 0f, default, 3);
            m_Manager.AddComponent<DeadTag>(target);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: target);

            RunSystem();

            var homing = m_Manager.GetComponentData<HomingData>(missile);
            Assert.AreNotEqual(target, homing.TargetEntity,
                "Мёртвая цель должна быть сброшена");
        }

        [Test]
        public void RotateData_FollowsMoveDirection_AfterSteer()
        {
            var target = CreateAsteroidEntity(new float2(0f, 10f), 0f, default, 3);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: target,
                turnRateRadPerSec: math.PI / 2f);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            var rotate = m_Manager.GetComponentData<RotateData>(missile);
            // RotateData используется GameObjectSyncSystem для поворота визуала.
            // После любого изменения курса оно должно совпадать с фактическим Direction.
            Assert.AreEqual(move.Direction.x, rotate.Rotation.x, 1e-4f);
            Assert.AreEqual(move.Direction.y, rotate.Rotation.y, 1e-4f);
        }

        [Test]
        public void RotateData_FollowsMoveDirection_WhenNoTarget()
        {
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(0f, 1f),
                target: Entity.Null,
                acquisitionRange: 1f);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            var rotate = m_Manager.GetComponentData<RotateData>(missile);
            Assert.AreEqual(move.Direction.x, rotate.Rotation.x, 1e-4f);
            Assert.AreEqual(move.Direction.y, rotate.Rotation.y, 1e-4f);
        }

        [Test]
        public void HandlesDestroyedTargetEntity_Gracefully()
        {
            var target = CreateAsteroidEntity(new float2(5f, 0f), 0f, default, 3);
            var missile = CreateMissileEntity(new float2(0f, 0f), new float2(1f, 0f),
                target: target);
            m_Manager.DestroyEntity(target);

            Assert.DoesNotThrow(() => RunSystem());

            var homing = m_Manager.GetComponentData<HomingData>(missile);
            Assert.AreEqual(Entity.Null, homing.TargetEntity);
        }
    }
}
