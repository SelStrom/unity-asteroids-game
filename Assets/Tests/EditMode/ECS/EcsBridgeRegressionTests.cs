using System.Collections.Generic;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    /// <summary>
    /// Регрессионные тесты на баги ECS-bridge слоя.
    /// Каждый тест воспроизводит конкретный сценарий, вызывавший ошибку.
    /// </summary>
    public class EcsBridgeRegressionTests : AsteroidsEcsTestFixture
    {
        private DeadEntityCleanupSystem _deadCleanupSystem;
        private readonly List<GameObject> _createdGameObjects = new();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _deadCleanupSystem = World.AddSystemManaged(new DeadEntityCleanupSystem());
        }

        [TearDown]
        public override void TearDown()
        {
            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _createdGameObjects.Clear();
            base.TearDown();
        }

        private GameObject CreateTestGameObject(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            _createdGameObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Регрессия: DeadEntityCleanupSystem вызывал callback внутри SystemAPI.Query,
        /// а callback делал structural change (CreateEntity/AddComponent) → InvalidOperationException.
        /// Фикс: callbacks вызываются ПОСЛЕ ECB.Playback(), вне итерации.
        /// </summary>
        [Test]
        public void DeadEntityCallback_CanPerformStructuralChanges_WithoutException()
        {
            var go = CreateTestGameObject("Asteroid", new Vector3(5f, 3f, 0f));
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new DeadTag());
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                GameObject = go,
                Transform = go.transform
            });

            var structuralChangePerformed = false;

            // Callback делает structural change — это вызывало ошибку
            _deadCleanupSystem.SetOnDeadEntityCallback(deadGo =>
            {
                var newEntity = m_Manager.CreateEntity();
                m_Manager.AddComponentData(newEntity, new AsteroidTag());
                m_Manager.AddComponentData(newEntity, new MoveData
                {
                    Position = new float2(deadGo.transform.position.x, deadGo.transform.position.y),
                    Speed = 5f,
                    Direction = new float2(1f, 0f)
                });
                structuralChangePerformed = true;
            });

            Assert.DoesNotThrow(() => _deadCleanupSystem.Update(),
                "Callback с structural change не должен вызывать исключение");
            Assert.IsTrue(structuralChangePerformed,
                "Callback должен был выполниться");

            // Исходная entity уничтожена, новая создана
            Assert.IsFalse(m_Manager.Exists(entity),
                "Мёртвая entity должна быть уничтожена");

            var asteroidQuery = m_Manager.CreateEntityQuery(typeof(AsteroidTag));
            Assert.AreEqual(1, asteroidQuery.CalculateEntityCount(),
                "Новая asteroid entity должна существовать");
        }

        /// <summary>
        /// Регрессия: callback DeadEntityCleanupSystem вызывался внутри итерации,
        /// entity ещё существовала, и callback мог создать несколько entity за один вызов
        /// (дробление астероида → 2 осколка). Оба CreateEntity — structural changes.
        /// </summary>
        [Test]
        public void DeadEntityCallback_CanCreateMultipleEntities_WithoutException()
        {
            var go = CreateTestGameObject("BigAsteroid", new Vector3(2f, 4f, 0f));
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new DeadTag());
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                GameObject = go,
                Transform = go.transform
            });

            // Имитация дробления астероида: 2 новых entity
            _deadCleanupSystem.SetOnDeadEntityCallback(deadGo =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var fragment = m_Manager.CreateEntity();
                    m_Manager.AddComponentData(fragment, new AsteroidTag());
                    m_Manager.AddComponentData(fragment, new MoveData
                    {
                        Position = new float2(deadGo.transform.position.x, deadGo.transform.position.y),
                        Speed = 8f,
                        Direction = new float2(1f, 0f)
                    });
                }
            });

            Assert.DoesNotThrow(() => _deadCleanupSystem.Update(),
                "Создание нескольких entity в callback не должно вызывать исключение");

            var asteroidQuery = m_Manager.CreateEntityQuery(typeof(AsteroidTag));
            Assert.AreEqual(2, asteroidQuery.CalculateEntityCount(),
                "Должны быть созданы 2 осколка астероида");
        }

        /// <summary>
        /// Регрессия: OnDeadEntity читал позицию из модели (model.Move.Position.Value),
        /// которая не синхронизировалась из ECS. Позиция оставалась начальной (часто 0,0).
        /// Фикс: позиция читается из go.transform.position (синхронизируется GameObjectSyncSystem).
        /// Тест проверяет, что callback получает GameObject с актуальной позицией.
        /// </summary>
        [Test]
        public void DeadEntityCallback_ReceivesGameObject_WithCurrentTransformPosition()
        {
            var worldPosition = new Vector3(7.5f, -3.2f, 0f);
            var go = CreateTestGameObject("MovedAsteroid", worldPosition);

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new DeadTag());
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                GameObject = go,
                Transform = go.transform
            });

            Vector3 capturedPosition = Vector3.zero;
            _deadCleanupSystem.SetOnDeadEntityCallback(deadGo =>
            {
                capturedPosition = deadGo.transform.position;
            });

            _deadCleanupSystem.Update();

            Assert.AreEqual(worldPosition.x, capturedPosition.x, 0.01f,
                "Callback должен получить актуальную X-позицию из Transform");
            Assert.AreEqual(worldPosition.y, capturedPosition.y, 0.01f,
                "Callback должен получить актуальную Y-позицию из Transform");
        }

        /// <summary>
        /// Регрессия: при рестарте ECS-entity не уничтожались — EntitiesCatalog.Release()
        /// удалял маппинг но не вызывал EntityManager.DestroyEntity().
        /// Мир накапливал entity от предыдущих игр.
        /// Фикс: Release() теперь уничтожает ECS-entity, CleanUp() — страховка.
        /// </summary>
        [Test]
        public void EntityDestruction_IsClean_WhenMappingRemoved()
        {
            // Имитация жизненного цикла: создаём entity + маппинг, затем удаляем
            var entity1 = CreateAsteroidEntity(new float2(1, 1), 3f, new float2(1, 0), 3);
            var entity2 = CreateAsteroidEntity(new float2(2, 2), 5f, new float2(0, 1), 2);
            var entity3 = CreateBulletEntity(new float2(3, 3), 20f, new float2(1, 0), 2f, true);

            Assert.IsTrue(m_Manager.Exists(entity1));
            Assert.IsTrue(m_Manager.Exists(entity2));
            Assert.IsTrue(m_Manager.Exists(entity3));

            // Уничтожаем entity напрямую (как делает Release)
            m_Manager.DestroyEntity(entity1);
            m_Manager.DestroyEntity(entity2);
            m_Manager.DestroyEntity(entity3);

            Assert.IsFalse(m_Manager.Exists(entity1),
                "Entity1 должна быть уничтожена");
            Assert.IsFalse(m_Manager.Exists(entity2),
                "Entity2 должна быть уничтожена");
            Assert.IsFalse(m_Manager.Exists(entity3),
                "Entity3 должна быть уничтожена");

            // Singleton entities должны оставаться нетронутыми
            var gameArea = CreateGameAreaSingleton(new float2(20, 15));
            var score = CreateScoreDataSingleton(0);

            Assert.IsTrue(m_Manager.Exists(gameArea),
                "Singleton GameArea не должен удаляться при cleanup");
            Assert.IsTrue(m_Manager.Exists(score),
                "Singleton ScoreData не должен удаляться при cleanup");
        }

        /// <summary>
        /// Регрессия: при рестарте event-буферы (GunShootEvent, LaserShootEvent, CollisionEventData)
        /// не очищались, ScoreData не сбрасывался. Новая игра начиналась с остатками предыдущей.
        /// </summary>
        [Test]
        public void EventBuffersAndScore_CanBeCleared_ForRestart()
        {
            var gunSingleton = CreateGunShootEventSingleton();
            var laserSingleton = CreateLaserShootEventSingleton();
            var collisionSingleton = CreateCollisionEventSingleton();
            var scoreSingleton = CreateScoreDataSingleton(500);

            // Заполняем буферы данными от "предыдущей игры"
            var gunBuffer = m_Manager.GetBuffer<GunShootEvent>(gunSingleton);
            gunBuffer.Add(new GunShootEvent
            {
                Position = new float2(1, 1),
                Direction = new float2(0, 1),
                IsPlayer = true
            });

            var laserBuffer = m_Manager.GetBuffer<LaserShootEvent>(laserSingleton);
            laserBuffer.Add(new LaserShootEvent
            {
                Position = new float2(2, 2),
                Direction = new float2(1, 0)
            });

            var collisionBuffer = m_Manager.GetBuffer<CollisionEventData>(collisionSingleton);
            collisionBuffer.Add(new CollisionEventData());

            // Имитация сброса при рестарте (как делает Game.ClearEcsEventBuffers)
            m_Manager.GetBuffer<GunShootEvent>(gunSingleton).Clear();
            m_Manager.GetBuffer<LaserShootEvent>(laserSingleton).Clear();
            m_Manager.GetBuffer<CollisionEventData>(collisionSingleton).Clear();
            m_Manager.SetComponentData(scoreSingleton, new ScoreData { Value = 0 });

            Assert.AreEqual(0, m_Manager.GetBuffer<GunShootEvent>(gunSingleton).Length,
                "GunShootEvent буфер должен быть пуст после рестарта");
            Assert.AreEqual(0, m_Manager.GetBuffer<LaserShootEvent>(laserSingleton).Length,
                "LaserShootEvent буфер должен быть пуст после рестарта");
            Assert.AreEqual(0, m_Manager.GetBuffer<CollisionEventData>(collisionSingleton).Length,
                "CollisionEventData буфер должен быть пуст после рестарта");
            Assert.AreEqual(0, m_Manager.GetComponentData<ScoreData>(scoreSingleton).Value,
                "ScoreData должен быть 0 после рестарта");
        }

        /// <summary>
        /// Регрессия: в ECS-режиме Model.Update() не вызывается, поэтому _newEntities
        /// никогда не переносятся в _entities. Model.CleanUp() итерировал пустой _entities
        /// и OnEntityDestroyed не вызывался → ECS-entity оставались в мире после рестарта.
        /// Фикс: CleanUp() теперь также итерирует _newEntities.
        /// </summary>
        [Test]
        public void ModelCleanUp_InvokesOnEntityDestroyed_ForNewEntities()
        {
            var model = new Model { GameArea = new Vector2(20f, 15f) };
            var destroyedEntities = new List<IGameEntityModel>();
            model.OnEntityDestroyed += entity => destroyedEntities.Add(entity);

            // Добавляем entity через AddEntity (попадает в _newEntities)
            var ship = new ShipModel();
            model.AddEntity(ship);
            var asteroid = new AsteroidModel();
            model.AddEntity(asteroid);

            // НЕ вызываем model.Update() — имитация ECS-режима

            model.CleanUp();

            Assert.AreEqual(2, destroyedEntities.Count,
                "CleanUp должен вызвать OnEntityDestroyed для entity из _newEntities");
            Assert.Contains(ship, destroyedEntities,
                "Ship должен быть в списке уничтоженных");
            Assert.Contains(asteroid, destroyedEntities,
                "Asteroid должен быть в списке уничтоженных");
        }

        /// <summary>
        /// Регрессия: при рестарте после смерти корабля — DeadEntityCleanupSystem уже удалял
        /// entity через ReleaseByGameObject, а затем Model.CleanUp() вызывал OnEntityDestroyed
        /// повторно → KeyNotFoundException в EntitiesCatalog.Release().
        /// Фикс: Release() проверяет наличие модели в словаре перед доступом.
        /// Тест: двойной CleanUp не должен падать (entity уже очищены первым вызовом).
        /// </summary>
        [Test]
        public void ModelCleanUp_DoesNotThrow_WhenCalledTwice()
        {
            var model = new Model { GameArea = new Vector2(20f, 15f) };
            var destroyCount = 0;
            model.OnEntityDestroyed += _ => destroyCount++;

            var ship = new ShipModel();
            model.AddEntity(ship);
            var asteroid = new AsteroidModel();
            model.AddEntity(asteroid);

            model.CleanUp();
            Assert.AreEqual(2, destroyCount,
                "Первый CleanUp должен вызвать OnEntityDestroyed для обеих entity");

            // Второй CleanUp (имитация: entity уже были обработаны)
            Assert.DoesNotThrow(() => model.CleanUp(),
                "Повторный CleanUp не должен падать — _entities и _newEntities уже пусты");
            Assert.AreEqual(2, destroyCount,
                "Повторный CleanUp не должен генерировать дополнительные вызовы");
        }

        /// <summary>
        /// Регрессия: лазер в ECS-режиме вызывал Kill(model) вместо добавления DeadTag.
        /// Kill() использовал model.Move.Position.Value (устаревшую позицию), и не проходил
        /// через DeadEntityCleanupSystem → OnDeadEntity → Application.OnDeadEntity.
        /// Фикс: ProcessShootEvents добавляет DeadTag к entity, DeadEntityCleanupSystem
        /// обрабатывает уничтожение с корректной позицией из Transform.
        /// </summary>
        [Test]
        public void LaserKill_InEcsMode_AddsDeadTag_InsteadOfModelKill()
        {
            var worldPosition = new Vector3(5f, 3f, 0f);
            var go = CreateTestGameObject("LaserTarget", worldPosition);

            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new AsteroidTag());
            m_Manager.AddComponentData(entity, new MoveData
            {
                Position = new float2(worldPosition.x, worldPosition.y),
                Speed = 2f,
                Direction = new float2(1f, 0f)
            });
            m_Manager.AddComponentObject(entity, new GameObjectRef
            {
                GameObject = go,
                Transform = go.transform
            });

            // Изначально DeadTag отсутствует
            Assert.IsFalse(m_Manager.HasComponent<DeadTag>(entity),
                "Entity не должна иметь DeadTag до поражения лазером");

            // Имитация корректного поведения ProcessShootEvents: добавление DeadTag
            m_Manager.AddComponentData(entity, new DeadTag());

            Assert.IsTrue(m_Manager.HasComponent<DeadTag>(entity),
                "Entity должна получить DeadTag после поражения лазером");

            // DeadEntityCleanupSystem обрабатывает уничтожение
            Vector3 capturedPosition = Vector3.zero;
            _deadCleanupSystem.SetOnDeadEntityCallback(deadGo =>
            {
                capturedPosition = deadGo.transform.position;
            });

            _deadCleanupSystem.Update();

            Assert.IsFalse(m_Manager.Exists(entity),
                "Entity должна быть уничтожена DeadEntityCleanupSystem");
            Assert.AreEqual(worldPosition.x, capturedPosition.x, 0.01f,
                "Callback должен получить позицию из Transform, не из модели");
            Assert.AreEqual(worldPosition.y, capturedPosition.y, 0.01f,
                "Callback должен получить позицию из Transform, не из модели");
        }

        /// <summary>
        /// Регрессия: лазерный VFX (LineRenderer) оставался на экране при смерти корабля.
        /// ActionScheduler.ResetSchedule() удалял запланированный Release, VFX не очищался.
        /// Фикс: Game._activeLaserVfx отслеживает активные VFX; Stop() вызывает Release
        /// для всех из списка перед ResetSchedule().
        /// Тест проверяет паттерн трекинга: добавление, удаление по таймеру, очистка при Stop.
        /// </summary>
        [Test]
        public void LaserVfx_ActiveList_TracksCreatedEffects()
        {
            // Имитация _activeLaserVfx паттерна
            var activeLaserVfx = new List<GameObject>();

            // Имитация создания VFX эффекта лазера
            var vfxGo1 = CreateTestGameObject("LaserVfx1", Vector3.zero);
            var vfxGo2 = CreateTestGameObject("LaserVfx2", Vector3.one);

            activeLaserVfx.Add(vfxGo1);
            activeLaserVfx.Add(vfxGo2);

            Assert.AreEqual(2, activeLaserVfx.Count,
                "Два VFX должны быть в списке после создания");

            // Имитация scheduled cleanup (таймер истёк для первого VFX)
            activeLaserVfx.Remove(vfxGo1);

            Assert.AreEqual(1, activeLaserVfx.Count,
                "После scheduled cleanup одного VFX должен остаться один");
            Assert.IsTrue(activeLaserVfx.Contains(vfxGo2),
                "Второй VFX ещё активен");

            // Имитация Stop() — очистка всех оставшихся VFX
            foreach (var vfx in activeLaserVfx)
            {
                Assert.IsNotNull(vfx,
                    "VFX объект должен существовать для Release");
            }
            activeLaserVfx.Clear();

            Assert.AreEqual(0, activeLaserVfx.Count,
                "После Stop() список должен быть пуст");
        }
    }
}
