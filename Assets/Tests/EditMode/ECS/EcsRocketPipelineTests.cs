using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using SelStrom.Asteroids.ECS;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    /// <summary>
    /// Интеграция: наведение (EcsHomingSystem) + движение (EcsMoveSystem) в связке.
    /// Проверяет, что ракета реально доворачивает на цель и сокращает дистанцию.
    /// </summary>
    public class EcsRocketPipelineTests : AsteroidsEcsTestFixture
    {
        private void Tick(SystemHandle homing, SystemHandle move, float dt)
        {
            World.PushTime(new TimeData(dt, dt));
            homing.Update(World.Unmanaged);
            move.Update(World.Unmanaged);
            World.PopTime();
        }

        [Test]
        public void Rocket_HomesAndClosesDistance_TowardAsteroid()
        {
            // Большая игровая зона, чтобы тороидальный перенос не искажал замер дистанции.
            CreateGameAreaSingleton(new float2(1000f, 1000f));
            var target = new float2(0f, 5f);
            var rocket = CreateRocketEntity(float2.zero, 5f, new float2(1f, 0f), turnRate: 180f);
            CreateAsteroidEntity(target, 0f, float2.zero, age: 3);

            var homing = World.CreateSystem<EcsHomingSystem>();
            var move = World.CreateSystem<EcsMoveSystem>();

            var startDist = math.distance(
                m_Manager.GetComponentData<MoveData>(rocket).Position, target);

            for (var i = 0; i < 30; i++)
            {
                Tick(homing, move, 0.1f);
            }

            var endPos = m_Manager.GetComponentData<MoveData>(rocket).Position;
            var endDist = math.distance(endPos, target);

            Assert.Less(endDist, startDist, "Ракета должна приближаться к цели");
            Assert.Greater(endPos.y, 0f, "Ракета должна доворачивать в сторону цели (вверх)");
        }
    }
}
