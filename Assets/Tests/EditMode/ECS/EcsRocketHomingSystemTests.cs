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

        private Entity CreateTargetEntity(float2 position)
        {
            var target = m_Manager.CreateEntity();
            m_Manager.AddComponentData(target, new MoveData
            {
                Position = position,
                Speed = 0f,
                Direction = float2.zero
            });
            return target;
        }

        [Test]
        public void Homing_RotatesDirection_TowardsTarget_WhenTurnRateIsLarge()
        {
            // Цель прямо над ракетой; turnRate большой => направление повернётся на 90° за 1 сек
            var target = CreateTargetEntity(new float2(0f, 5f));
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: math.PI,
                target: target);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, move.Direction.x, 1e-3f);
            Assert.AreEqual(1f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void Homing_LimitsRotation_ByTurnRate_PerStep()
        {
            // Цель сзади (180° относительно направления (1,0)) — позиция (-5, 0)
            // turnRate = PI/2 рад/сек, dt = 0.5 сек => max поворот = PI/4 = 45°
            var target = CreateTargetEntity(new float2(-5f, 0f));
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: math.PI * 0.5f,
                target: target);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);

            // Угол между новым направлением и осью X должен быть ровно 45°
            // (знак зависит от того, в какую сторону поворачивается, но модуль == PI/4)
            var angleFromX = math.atan2(move.Direction.y, move.Direction.x);
            Assert.AreEqual(math.PI * 0.25f, math.abs(angleFromX), 1e-3f);

            // Длина вектора = 1
            Assert.AreEqual(1f, math.length(move.Direction), 1e-3f);
        }

        [Test]
        public void Homing_KeepsDirection_WhenTargetEntityIsNull()
        {
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: math.PI,
                target: Entity.Null);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(new float2(1f, 0f), move.Direction);
        }

        [Test]
        public void Homing_KeepsDirection_WhenTargetDestroyed()
        {
            var target = CreateTargetEntity(new float2(0f, 5f));
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: math.PI,
                target: target);

            // Убиваем цель до запуска системы
            m_Manager.DestroyEntity(target);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(new float2(1f, 0f), move.Direction);
        }

        [Test]
        public void Homing_KeepsDirection_WhenTargetHasNoMoveData()
        {
            var brokenTarget = m_Manager.CreateEntity();
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 10f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: math.PI,
                target: brokenTarget);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(new float2(1f, 0f), move.Direction);
        }

        [Test]
        public void Homing_DoesNotChange_WhenTargetIsAhead_AndAligned()
        {
            // Цель прямо по направлению полёта
            var target = CreateTargetEntity(new float2(10f, 0f));
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 5f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: math.PI,
                target: target);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, move.Direction.x, 1e-3f);
            Assert.AreEqual(0f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void Homing_HandlesZeroTurnRate_WithoutError()
        {
            var target = CreateTargetEntity(new float2(0f, 5f));
            var rocket = CreateRocketEntity(
                position: float2.zero,
                speed: 5f,
                direction: new float2(1f, 0f),
                turnRateRadPerSec: 0f,
                target: target);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(new float2(1f, 0f), move.Direction);
        }
    }
}
