using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class HomingSystemTests : AsteroidsEcsTestFixture
    {
        private Entity _missileEntity;
        private SystemHandle _systemHandle;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _systemHandle = World.CreateSystem<EcsHomingSystem>();
        }

        private Entity CreateMissileEntity(float2 position, float speed, float2 direction, float turnSpeed)
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new MissileTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.AddComponentData(entity, new HomingData
            {
                TurnSpeed = turnSpeed
            });
            return entity;
        }

        private void RunSystem(float deltaTime = 1.0f)
        {
            World.PushTime(new TimeData(deltaTime, deltaTime));
            _systemHandle.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Direction_TurnsToward_NearestAsteroid()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 180f);

            CreateAsteroidEntity(new float2(0f, 5f), 1f, new float2(1f, 0f), 3);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            Assert.Greater(move.Direction.y, 0f, "Ракета должна повернуть в сторону астероида (вверх)");
        }

        [Test]
        public void Direction_TurnsToward_NearestUfo()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 180f);

            CreateUfoEntity(new float2(0f, -5f), 1f, new float2(1f, 0f));

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            Assert.Less(move.Direction.y, 0f, "Ракета должна повернуть в сторону UFO (вниз)");
        }

        [Test]
        public void Direction_TurnsToward_NearestUfoBig()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 180f);

            CreateUfoBigEntity(new float2(-5f, 0f), 1f, new float2(1f, 0f));

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            Assert.Less(move.Direction.x, 1f, "Ракета должна повернуть в сторону UfoBig (влево)");
        }

        [Test]
        public void Direction_ChoosesNearest_WhenMultipleTargets()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 360f);

            CreateAsteroidEntity(new float2(10f, 0f), 1f, new float2(1f, 0f), 3);
            CreateAsteroidEntity(new float2(3f, 0f), 1f, new float2(1f, 0f), 3);

            RunSystem(0.01f);

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            Assert.Greater(move.Direction.x, 0.9f,
                "Ракета должна лететь к ближайшему астероиду (вправо)");
        }

        [Test]
        public void Direction_Unchanged_WhenNoTargets()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 180f);

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            Assert.AreEqual(1f, move.Direction.x, 0.01f);
            Assert.AreEqual(0f, move.Direction.y, 0.01f);
        }

        [Test]
        public void TurnSpeed_LimitsRotation()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 10f);

            CreateAsteroidEntity(new float2(0f, 5f), 1f, new float2(1f, 0f), 3);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            Assert.Greater(move.Direction.x, 0.9f,
                "С низким TurnSpeed ракета не должна резко повернуть на 90 градусов");
            Assert.Greater(move.Direction.y, 0f,
                "Но должна начать поворачивать вверх");
        }

        [Test]
        public void Direction_IsNormalized_AfterTurn()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 180f);

            CreateAsteroidEntity(new float2(3f, 4f), 1f, new float2(1f, 0f), 3);

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            var length = math.length(move.Direction);
            Assert.AreEqual(1f, length, 0.01f, "Направление должно быть нормализовано");
        }

        [Test]
        public void IgnoresDeadEntities()
        {
            _missileEntity = CreateMissileEntity(
                float2.zero, 5f, new float2(1f, 0f), 180f);

            var asteroid = CreateAsteroidEntity(new float2(0f, 5f), 1f, new float2(1f, 0f), 3);
            m_Manager.AddComponentData(asteroid, new DeadTag());

            RunSystem(0.1f);

            var move = m_Manager.GetComponentData<MoveData>(_missileEntity);
            Assert.AreEqual(1f, move.Direction.x, 0.01f,
                "Мёртвые цели игнорируются — направление не меняется");
            Assert.AreEqual(0f, move.Direction.y, 0.01f);
        }
    }
}
