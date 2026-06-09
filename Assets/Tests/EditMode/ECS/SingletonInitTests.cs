using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class SingletonInitTests : AsteroidsEcsTestFixture
    {
        // Helper -- повторяет idempotent паттерн из Application.InitializeEcsSingletons()
        // для ShipPositionData. Тестируем ПАТТЕРН, а не private метод.
        private void CreateShipPositionDataSingleton()
        {
            var query = m_Manager.CreateEntityQuery(typeof(ShipPositionData));
            if (query.CalculateEntityCount() == 0)
            {
                var entity = m_Manager.CreateEntity();
                m_Manager.AddComponentData(entity, new ShipPositionData
                {
                    Position = float2.zero,
                    Speed = 0f,
                    Direction = float2.zero
                });
            }
            else
            {
                var existingEntity = query.GetSingletonEntity();
                m_Manager.SetComponentData(existingEntity, new ShipPositionData
                {
                    Position = float2.zero,
                    Speed = 0f,
                    Direction = float2.zero
                });
            }
        }

        [Test]
        public void InitializeEcsSingletons_CreatesShipPositionData()
        {
            CreateShipPositionDataSingleton();
            var query = m_Manager.CreateEntityQuery(typeof(ShipPositionData));
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        [Test]
        public void InitializeEcsSingletons_ShipPositionData_DefaultValues()
        {
            CreateShipPositionDataSingleton();
            var query = m_Manager.CreateEntityQuery(typeof(ShipPositionData));
            var data = query.GetSingleton<ShipPositionData>();
            Assert.AreEqual(float2.zero, data.Position);
            Assert.AreEqual(0f, data.Speed);
            Assert.AreEqual(float2.zero, data.Direction);
        }

        [Test]
        public void InitializeEcsSingletons_ShipPositionData_IdempotentReInit()
        {
            CreateShipPositionDataSingleton();
            CreateShipPositionDataSingleton(); // повторный вызов
            var query = m_Manager.CreateEntityQuery(typeof(ShipPositionData));
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        // Повторяет idempotent паттерн из Application.InitializeEcsSingletons()
        // для RocketLaunchEvent buffer singleton.
        private void CreateRocketLaunchEventBufferSingleton()
        {
            var query = m_Manager.CreateEntityQuery(typeof(RocketLaunchEvent));
            if (query.CalculateEntityCount() == 0)
            {
                var entity = m_Manager.CreateEntity();
                m_Manager.AddBuffer<RocketLaunchEvent>(entity);
            }
            else
            {
                var existingEntity = query.GetSingletonEntity();
                m_Manager.GetBuffer<RocketLaunchEvent>(existingEntity).Clear();
            }
        }

        [Test]
        public void InitializeEcsSingletons_CreatesRocketLaunchEventBuffer()
        {
            CreateRocketLaunchEventBufferSingleton();
            var query = m_Manager.CreateEntityQuery(typeof(RocketLaunchEvent));
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        [Test]
        public void InitializeEcsSingletons_RocketLaunchEvent_IdempotentReInit()
        {
            CreateRocketLaunchEventBufferSingleton();
            CreateRocketLaunchEventBufferSingleton(); // повторный вызов
            var query = m_Manager.CreateEntityQuery(typeof(RocketLaunchEvent));
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        [Test]
        public void InitializeEcsSingletons_RocketLaunchEvent_ClearsBufferOnReInit()
        {
            CreateRocketLaunchEventBufferSingleton();

            // Добавляем событие в буфер
            var entity = m_Manager.CreateEntityQuery(typeof(RocketLaunchEvent)).GetSingletonEntity();
            m_Manager.GetBuffer<RocketLaunchEvent>(entity).Add(new RocketLaunchEvent
            {
                Position = new Unity.Mathematics.float2(1f, 2f),
                Direction = new Unity.Mathematics.float2(1f, 0f)
            });

            // Повторная инициализация должна очистить буфер
            CreateRocketLaunchEventBufferSingleton();

            var buffer = m_Manager.GetBuffer<RocketLaunchEvent>(
                m_Manager.CreateEntityQuery(typeof(RocketLaunchEvent)).GetSingletonEntity()
            );
            Assert.AreEqual(0, buffer.Length);
        }
    }
}
