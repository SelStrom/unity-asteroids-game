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

        private Entity CreateRocket(float2 position, float2 direction,
            float turnSpeedDegPerSec = 90f)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new RocketTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = 5f,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = direction
            });
            m_Manager.AddComponentData(entity, new HomingData
            {
                TurnSpeedDegPerSec = turnSpeedDegPerSec,
                TargetEntity = Entity.Null
            });
            return entity;
        }

        [Test]
        public void AcquiresNearestTarget_AmongAsteroids()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));
            CreateAsteroidEntity(new float2(10f, 0f), 0f, new float2(1f, 0f), 1);
            var near = CreateAsteroidEntity(new float2(3f, 0f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.01f);

            var homing = m_Manager.GetComponentData<HomingData>(rocket);
            Assert.AreEqual(near, homing.TargetEntity);
        }

        [Test]
        public void AcquiresUfo_AsTarget()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));
            var ufo = CreateUfoEntity(new float2(4f, 0f), 0f, new float2(1f, 0f));

            RunSystem(0.01f);

            var homing = m_Manager.GetComponentData<HomingData>(rocket);
            Assert.AreEqual(ufo, homing.TargetEntity);
        }

        [Test]
        public void AcquiresUfoBig_AsTarget()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));
            var ufoBig = CreateUfoBigEntity(new float2(4f, 0f), 0f, new float2(1f, 0f));

            RunSystem(0.01f);

            var homing = m_Manager.GetComponentData<HomingData>(rocket);
            Assert.AreEqual(ufoBig, homing.TargetEntity);
        }

        [Test]
        public void TurnsTowardTarget_LimitedByTurnSpeed()
        {
            // Цель строго вверх (90°), скорость поворота 90°/сек, dt = 0.5 → доворот на 45°
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f), 90f);
            CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var expected = math.normalize(new float2(1f, 1f));
            Assert.AreEqual(expected.x, move.Direction.x, 1e-3f);
            Assert.AreEqual(expected.y, move.Direction.y, 1e-3f);
        }

        [Test]
        public void DoesNotOvershoot_WhenTargetWithinTurnLimit()
        {
            // Цель строго вверх, поворот за кадр может быть до 180° — доворачивает ровно на цель
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f), 180f);
            CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), 1);

            RunSystem(1f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, move.Direction.x, 1e-3f);
            Assert.AreEqual(1f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void Direction_StaysNormalized()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f), 45f);
            CreateAsteroidEntity(new float2(-3f, 4f), 0f, new float2(1f, 0f), 1);

            for (var i = 0; i < 10; i++)
            {
                RunSystem(0.1f);
            }

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, math.length(move.Direction), 1e-3f);
        }

        [Test]
        public void Rotation_FollowsDirection()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f), 90f);
            CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            var rotate = m_Manager.GetComponentData<RotateData>(rocket);
            Assert.AreEqual(move.Direction.x, rotate.Rotation.x, 1e-4f);
            Assert.AreEqual(move.Direction.y, rotate.Rotation.y, 1e-4f);
        }

        [Test]
        public void KeepsTarget_WhileTargetAlive_EvenIfAnotherBecomesCloser()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));
            var first = CreateAsteroidEntity(new float2(3f, 0f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.01f);

            // Появился более близкий враг — цель не меняется
            CreateAsteroidEntity(new float2(1f, 0f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.01f);

            var homing = m_Manager.GetComponentData<HomingData>(rocket);
            Assert.AreEqual(first, homing.TargetEntity);
        }

        [Test]
        public void Retargets_WhenTargetIsDead()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));
            var first = CreateAsteroidEntity(new float2(3f, 0f), 0f, new float2(1f, 0f), 1);
            var second = CreateAsteroidEntity(new float2(6f, 0f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.01f);
            Assert.AreEqual(first,
                m_Manager.GetComponentData<HomingData>(rocket).TargetEntity);

            m_Manager.AddComponent<DeadTag>(first);
            RunSystem(0.01f);

            var homing = m_Manager.GetComponentData<HomingData>(rocket);
            Assert.AreEqual(second, homing.TargetEntity);
        }

        [Test]
        public void Retargets_WhenTargetIsDestroyed()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));
            var first = CreateAsteroidEntity(new float2(3f, 0f), 0f, new float2(1f, 0f), 1);
            var second = CreateAsteroidEntity(new float2(6f, 0f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.01f);
            Assert.AreEqual(first,
                m_Manager.GetComponentData<HomingData>(rocket).TargetEntity);

            m_Manager.DestroyEntity(first);
            RunSystem(0.01f);

            var homing = m_Manager.GetComponentData<HomingData>(rocket);
            Assert.AreEqual(second, homing.TargetEntity);
        }

        [Test]
        public void NoTargets_KeepsDirection()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(new float2(1f, 0f), move.Direction);
            Assert.AreEqual(Entity.Null,
                m_Manager.GetComponentData<HomingData>(rocket).TargetEntity);
        }

        [Test]
        public void DeadEnemies_AreNotAcquired()
        {
            var rocket = CreateRocket(float2.zero, new float2(1f, 0f));
            var dead = CreateAsteroidEntity(new float2(1f, 0f), 0f, new float2(1f, 0f), 1);
            m_Manager.AddComponent<DeadTag>(dead);
            var alive = CreateAsteroidEntity(new float2(5f, 0f), 0f, new float2(1f, 0f), 1);

            RunSystem(0.01f);

            var homing = m_Manager.GetComponentData<HomingData>(rocket);
            Assert.AreEqual(alive, homing.TargetEntity);
        }
    }
}
