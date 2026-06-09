using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class RocketFactoryAndCollisionTests : AsteroidsEcsTestFixture
    {
        [Test]
        public void CreateRocket_HasExpectedComponents()
        {
            var entity = CreateRocketEntity(
                new float2(1f, 2f), 8f, new float2(0f, 1f));

            Assert.IsTrue(m_Manager.HasComponent<RocketTag>(entity));
            Assert.IsTrue(m_Manager.HasComponent<PlayerBulletTag>(entity),
                "Ракета должна считаться пулей игрока для системы коллизий");
            Assert.IsTrue(m_Manager.HasComponent<MoveData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<RotateData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<HomingData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<LifeTimeData>(entity));
        }

        [Test]
        public void CreateRocket_SetsValuesFromArguments()
        {
            var entity = CreateRocketEntity(
                new float2(1f, 2f), 8f, new float2(0f, 1f),
                turnSpeedDegPerSec: 120f, lifeTime: 7f);

            var move = m_Manager.GetComponentData<MoveData>(entity);
            Assert.AreEqual(new float2(1f, 2f), move.Position);
            Assert.AreEqual(8f, move.Speed);
            Assert.AreEqual(new float2(0f, 1f), move.Direction);

            var rotate = m_Manager.GetComponentData<RotateData>(entity);
            Assert.AreEqual(new float2(0f, 1f), rotate.Rotation);
            Assert.AreEqual(0f, rotate.TargetDirection);

            var homing = m_Manager.GetComponentData<HomingData>(entity);
            Assert.AreEqual(120f, homing.TurnSpeedDegPerSec);
            Assert.AreEqual(Entity.Null, homing.TargetEntity);

            var lifeTime = m_Manager.GetComponentData<LifeTimeData>(entity);
            Assert.AreEqual(7f, lifeTime.TimeRemaining);
        }

        [Test]
        public void RocketCollidesWithAsteroid_BothDie_ScoreAdded()
        {
            var scoreEntity = CreateScoreDataSingleton();
            var bufferEntity = CreateCollisionEventSingleton();

            var rocket = CreateRocketEntity(float2.zero, 8f, new float2(1f, 0f));
            var asteroid = CreateAsteroidEntity(
                new float2(1f, 0f), 1f, new float2(-1f, 0f), 3, score: 250);

            var events = m_Manager.GetBuffer<CollisionEventData>(bufferEntity);
            events.Add(new CollisionEventData { EntityA = rocket, EntityB = asteroid });

            var system = World.CreateSystem<EcsCollisionHandlerSystem>();
            system.Update(World.Unmanaged);

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid));
            Assert.AreEqual(250,
                m_Manager.GetComponentData<ScoreData>(scoreEntity).Value);
        }

        [Test]
        public void RocketCollidesWithUfo_BothDie_ScoreAdded()
        {
            var scoreEntity = CreateScoreDataSingleton();
            var bufferEntity = CreateCollisionEventSingleton();

            var rocket = CreateRocketEntity(float2.zero, 8f, new float2(1f, 0f));
            var ufo = CreateUfoEntity(new float2(1f, 0f), 1f, new float2(-1f, 0f), score: 500);

            var events = m_Manager.GetBuffer<CollisionEventData>(bufferEntity);
            events.Add(new CollisionEventData { EntityA = ufo, EntityB = rocket });

            var system = World.CreateSystem<EcsCollisionHandlerSystem>();
            system.Update(World.Unmanaged);

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket));
            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(ufo));
            Assert.AreEqual(500,
                m_Manager.GetComponentData<ScoreData>(scoreEntity).Value);
        }

        [Test]
        public void RocketExpires_ByLifeTime()
        {
            var rocket = CreateRocketEntity(
                float2.zero, 8f, new float2(1f, 0f), lifeTime: 0.5f);

            var lifeTimeSystem = World.CreateSystem<EcsLifeTimeSystem>();
            var deadSystem = World.CreateSystemManaged<EcsDeadByLifeTimeSystem>();

            World.PushTime(new Unity.Core.TimeData(1.0, 1f));
            lifeTimeSystem.Update(World.Unmanaged);
            deadSystem.Update();
            World.PopTime();

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
                "Ракета должна умирать по истечении времени жизни");
        }
    }
}
