using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class EcsHomingSystemTests : AsteroidsEcsTestFixture
    {
        private void RunSystem(float deltaTime = 1.0f)
        {
            var system = CreateAndGetSystem<EcsHomingSystem>();
            var handle = World.GetExistingSystem<EcsHomingSystem>();
            World.PushTime(new TimeData(deltaTime, deltaTime));
            system.OnUpdate(ref World.Unmanaged.ResolveSystemStateRef(handle));
            World.PopTime();
        }

        private static float Angle(float2 dir)
        {
            return math.degrees(math.atan2(dir.y, dir.x));
        }

        [Test]
        public void NoEnemies_DirectionUnchanged()
        {
            var rocket = CreateRocketEntity(
                float2.zero, 10f, new float2(1f, 0f), turnRate: 90f);

            RunSystem();

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(1f, move.Direction.x, 0.0001f);
            Assert.AreEqual(0f, move.Direction.y, 0.0001f);
        }

        [Test]
        public void EnemyToSide_LargeTurnRate_SnapsToTarget()
        {
            var rocket = CreateRocketEntity(
                float2.zero, 10f, new float2(1f, 0f), turnRate: 720f);
            // Враг строго сверху: цель под 90° от текущего направления (+x).
            CreateAsteroidEntity(new float2(0f, 5f), 0f, float2.zero, age: 3);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            // Поворот 720°/сек за 1с с лихвой перекрывает 90° → направление точно на цель (вверх).
            Assert.AreEqual(0f, move.Direction.x, 0.001f);
            Assert.AreEqual(1f, move.Direction.y, 0.001f);
        }

        [Test]
        public void EnemyToSide_SmallTurnRate_ArcLimited()
        {
            var rocket = CreateRocketEntity(
                float2.zero, 10f, new float2(1f, 0f), turnRate: 10f);
            // Цель под 90° (сверху), но за кадр можно повернуть лишь на 10°.
            CreateAsteroidEntity(new float2(0f, 5f), 0f, float2.zero, age: 3);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            // Дуга: направление повернулось ровно на 10°, а не на все 90°.
            Assert.AreEqual(10f, Angle(move.Direction), 0.01f);
            Assert.AreEqual(1f, math.length(move.Direction), 0.001f);
        }

        [Test]
        public void PicksNearestEnemy()
        {
            var rocket = CreateRocketEntity(
                float2.zero, 10f, new float2(1f, 0f), turnRate: 720f);
            // Дальний враг сверху, ближний снизу — ракета должна выбрать ближнего (вниз).
            CreateAsteroidEntity(new float2(0f, 10f), 0f, float2.zero, age: 3);
            CreateAsteroidEntity(new float2(0f, -2f), 0f, float2.zero, age: 3);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, move.Direction.x, 0.001f);
            Assert.AreEqual(-1f, move.Direction.y, 0.001f);
        }

        [Test]
        public void TargetsUfo_NotOnlyAsteroids()
        {
            var rocket = CreateRocketEntity(
                float2.zero, 10f, new float2(1f, 0f), turnRate: 720f);
            // Единственная цель — UFO сверху.
            CreateUfoEntity(new float2(0f, 5f), 0f, float2.zero);

            RunSystem(1.0f);

            var move = m_Manager.GetComponentData<MoveData>(rocket);
            Assert.AreEqual(0f, move.Direction.x, 0.001f);
            Assert.AreEqual(1f, move.Direction.y, 0.001f);
        }
    }
}
