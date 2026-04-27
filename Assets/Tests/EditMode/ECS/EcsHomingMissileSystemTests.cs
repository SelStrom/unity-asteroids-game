using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsHomingMissileSystemTests : AsteroidsEcsTestFixture
    {
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsHomingMissileSystem>();
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        private Entity CreateMissile(float2 position, float2 direction,
            float turnRateRadPerSec, float seekRange, float speed = 5f)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileTag());
            m_Manager.AddComponentData(entity, new PlayerMissileTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new RotateData
            {
                Rotation = direction
            });
            m_Manager.AddComponentData(entity, new HomingMissileData
            {
                TurnRateRadPerSec = turnRateRadPerSec,
                SeekRange = seekRange
            });
            return entity;
        }

        [Test]
        public void NoTargets_KeepsDirectionUnchanged()
        {
            var initialDir = math.normalize(new float2(1f, 0f));
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI, seekRange: 100f);

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(missile);
            Assert.AreEqual(initialDir.x, move.Direction.x, 1e-4f);
            Assert.AreEqual(initialDir.y, move.Direction.y, 1e-4f);
        }

        [Test]
        public void TargetOutsideSeekRange_KeepsDirectionUnchanged()
        {
            var initialDir = math.normalize(new float2(1f, 0f));
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI, seekRange: 5f);

            // Asteroid за пределами SeekRange (на расстоянии 100)
            CreateAsteroidEntity(new float2(0f, 100f), 0f, new float2(1f, 0f), age: 1);

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(missile);
            Assert.AreEqual(initialDir.x, move.Direction.x, 1e-4f);
            Assert.AreEqual(initialDir.y, move.Direction.y, 1e-4f);
        }

        [Test]
        public void TargetWithinRange_RotatesDirectionTowardsTarget()
        {
            var initialDir = new float2(1f, 0f);
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI, seekRange: 100f);

            // Цель прямо вверху — желаемое направление (0, 1).
            // turnRate = 180°/sec, dt = 0.5s → можно повернуть на 90°.
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), age: 1);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            Assert.AreEqual(0f, move.Direction.x, 1e-3f);
            Assert.AreEqual(1f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void TurnRateClamp_LimitsRotationPerFrame()
        {
            var initialDir = new float2(1f, 0f);
            // turnRate = π/4 рад/сек = 45°/сек, dt=1s → не больше 45°.
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI / 4f, seekRange: 100f);

            // Цель сверху → желаемый поворот 90°, но за 1 сек разрешено только 45°.
            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), age: 1);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            var resultAngle = math.atan2(move.Direction.y, move.Direction.x);
            Assert.AreEqual(math.PI / 4f, resultAngle, 1e-3f);
        }

        [Test]
        public void PicksNearestTarget_AmongMultiple()
        {
            var initialDir = new float2(1f, 0f);
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI * 2f, seekRange: 100f);

            // Дальняя цель сверху, ближняя цель справа
            CreateAsteroidEntity(new float2(0f, 50f), 0f, new float2(1f, 0f), age: 1);
            var near = new float2(2f, 0f);
            CreateAsteroidEntity(near, 0f, new float2(1f, 0f), age: 1);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            // Цель прямо по курсу — направление не меняется (или меняется минимально)
            Assert.AreEqual(1f, move.Direction.x, 1e-3f);
            Assert.AreEqual(0f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void IgnoresDeadTargets()
        {
            var initialDir = new float2(1f, 0f);
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI, seekRange: 100f);

            // Мёртвая цель прямо вверху, живая цель прямо по курсу (на 10 единиц)
            var dead = CreateAsteroidEntity(new float2(0f, 5f), 0f, new float2(1f, 0f), age: 1);
            m_Manager.AddComponent<DeadTag>(dead);
            CreateAsteroidEntity(new float2(10f, 0f), 0f, new float2(1f, 0f), age: 1);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            // Должна целиться в (10,0) — то есть остаться по направлению (1,0)
            Assert.AreEqual(1f, move.Direction.x, 1e-3f);
            Assert.AreEqual(0f, move.Direction.y, 1e-3f);
        }

        [Test]
        public void TargetWithinRange_SyncsRotationWithDirection()
        {
            var initialDir = new float2(1f, 0f);
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI, seekRange: 100f);

            CreateAsteroidEntity(new float2(0f, 10f), 0f, new float2(1f, 0f), age: 1);

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            var rotate = m_Manager.GetComponentData<RotateData>(missile);
            Assert.AreEqual(move.Direction.x, rotate.Rotation.x, 1e-4f);
            Assert.AreEqual(move.Direction.y, rotate.Rotation.y, 1e-4f);
        }

        [Test]
        public void NoTargets_SyncsRotationWithCurrentDirection()
        {
            var initialDir = math.normalize(new float2(0.6f, 0.8f));
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI, seekRange: 100f);
            // Намеренно сбиваем RotateData, чтобы убедиться, что система его подравнивает.
            m_Manager.SetComponentData(missile, new RotateData
            {
                Rotation = new float2(1f, 0f)
            });

            RunSystem();

            var rotate = m_Manager.GetComponentData<RotateData>(missile);
            Assert.AreEqual(initialDir.x, rotate.Rotation.x, 1e-4f);
            Assert.AreEqual(initialDir.y, rotate.Rotation.y, 1e-4f);
        }

        [Test]
        public void TargetsUfo_AlongsideAsteroids()
        {
            var initialDir = new float2(1f, 0f);
            var missile = CreateMissile(float2.zero, initialDir,
                turnRateRadPerSec: math.PI, seekRange: 100f);

            // UFO прямо вверху, без астероидов — наводимся на UFO
            CreateUfoEntity(new float2(0f, 10f), 0f, new float2(1f, 0f));

            RunSystem(0.5f);

            var move = m_Manager.GetComponentData<MoveData>(missile);
            Assert.AreEqual(0f, move.Direction.x, 1e-3f);
            Assert.AreEqual(1f, move.Direction.y, 1e-3f);
        }
    }
}
