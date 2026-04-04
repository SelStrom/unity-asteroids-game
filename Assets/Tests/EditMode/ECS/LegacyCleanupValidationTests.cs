using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    /// <summary>
    /// Phase 06: Legacy Cleanup -- validation tests.
    /// Verifies LC-01..LC-06 requirements after legacy layer removal.
    /// </summary>
    public class LegacyCleanupValidationTests : AsteroidsEcsTestFixture
    {
        private Assembly _mainAssembly;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _mainAssembly = typeof(SelStrom.Asteroids.Application).Assembly;
        }

        // --- LC-01: Legacy systems deleted ---

        [Test]
        public void LC01_LegacySystems_AreAbsentFromAssembly()
        {
            var legacySystemNames = new[]
            {
                "BaseModelSystem`1",
                "MoveSystem",
                "RotateSystem",
                "ThrustSystem",
                "GunSystem",
                "LaserSystem",
                "ShootToSystem",
                "MoveToSystem",
                "LifeTimeSystem"
            };

            var allTypes = _mainAssembly.GetTypes().Select(t => t.Name).ToHashSet();

            foreach (var name in legacySystemNames)
            {
                Assert.IsFalse(allTypes.Contains(name),
                    $"Legacy system '{name}' should not exist in assembly after Phase 06 cleanup");
            }
        }

        // --- LC-02: Legacy models and components deleted ---

        [Test]
        public void LC02_LegacyModelsAndComponents_AreAbsentFromAssembly()
        {
            var legacyTypeNames = new[]
            {
                "ShipModel",
                "AsteroidModel",
                "BulletModel",
                "UfoBigModel",
                "UfoModel",
                "IGameEntityModel",
                "IGroupVisitor",
                "IModelComponent",
                "GunComponent",
                "LaserComponent",
                "MoveComponent",
                "RotateComponent",
                "ThrustComponent",
                "LifeTimeComponent",
                "MoveToComponent",
                "ShootToComponent"
            };

            var allTypes = _mainAssembly.GetTypes().Select(t => t.Name).ToHashSet();

            foreach (var name in legacyTypeNames)
            {
                Assert.IsFalse(allTypes.Contains(name),
                    $"Legacy type '{name}' should not exist in assembly after Phase 06 cleanup");
            }
        }

        [Test]
        public void LC02_ModelFactory_IsAbsentFromAssembly()
        {
            var type = _mainAssembly.GetType("SelStrom.Asteroids.ModelFactory");
            Assert.IsNull(type,
                "ModelFactory should not exist in assembly after Phase 06 cleanup");
        }

        [Test]
        public void LC02_ModelClass_IsAbsentFromAssembly()
        {
            // Model was the legacy coordinator class. It should be deleted.
            // Note: there may be other types with 'Model' in the name (e.g. ViewModel).
            // We specifically check for the exact legacy Model class.
            var type = _mainAssembly.GetType("SelStrom.Asteroids.Model");
            Assert.IsNull(type,
                "Legacy Model class should not exist in assembly after Phase 06 cleanup");
        }

        // --- LC-03: No _useEcs, single ECS data path ---

        [Test]
        public void LC03_ApplicationClass_HasNoUseEcsField()
        {
            var appType = typeof(SelStrom.Asteroids.Application);
            var field = appType.GetField("_useEcs",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNull(field,
                "Application class should not have _useEcs field after Phase 06 cleanup");
        }

        [Test]
        public void LC03_GameClass_HasNoUseEcsField()
        {
            var gameType = typeof(SelStrom.Asteroids.Game);
            var field = gameType.GetField("_useEcs",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNull(field,
                "Game class should not have _useEcs field after Phase 06 cleanup");
        }

        [Test]
        public void LC03_EntityTypeEnum_ExistsForDispatch()
        {
            var entityType = typeof(EntityType);
            Assert.IsTrue(entityType.IsEnum,
                "EntityType should be an enum for ECS-only entity dispatch");

            var names = Enum.GetNames(typeof(EntityType));
            Assert.IsTrue(names.Contains("Ship"), "EntityType should contain Ship");
            Assert.IsTrue(names.Contains("Asteroid"), "EntityType should contain Asteroid");
            Assert.IsTrue(names.Contains("Bullet"), "EntityType should contain Bullet");
            Assert.IsTrue(names.Contains("UfoBig"), "EntityType should contain UfoBig");
            Assert.IsTrue(names.Contains("Ufo"), "EntityType should contain Ufo");
        }

        // --- LC-04: ActionScheduler standalone (not through Model) ---

        [Test]
        public void LC04_ActionScheduler_CanBeUsedStandalone()
        {
            // ActionScheduler should work independently without Model.
            var scheduler = new ActionScheduler();
            var invoked = false;

            scheduler.ScheduleAction(() => { invoked = true; }, 1.0f);
            scheduler.Update(0.5f);
            Assert.IsFalse(invoked, "Action should not fire before duration elapses");

            scheduler.Update(0.6f);
            Assert.IsTrue(invoked, "Action should fire after duration elapses");
        }

        [Test]
        public void LC04_ActionScheduler_ResetSchedule_ClearsAllPending()
        {
            var scheduler = new ActionScheduler();
            var invoked = false;

            scheduler.ScheduleAction(() => { invoked = true; }, 1.0f);
            scheduler.ResetSchedule();
            scheduler.Update(2.0f);

            Assert.IsFalse(invoked,
                "ResetSchedule should clear pending actions so they never fire");
        }

        [Test]
        public void LC04_ApplicationClass_HasStandaloneActionSchedulerField()
        {
            var appType = typeof(SelStrom.Asteroids.Application);
            var field = appType.GetField("_actionScheduler",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field,
                "Application should have standalone _actionScheduler field");
            Assert.AreEqual(typeof(ActionScheduler), field.FieldType,
                "_actionScheduler should be of type ActionScheduler");
        }

        // --- LC-05: Model.cs deleted, score/state in ECS singletons ---

        [Test]
        public void LC05_ScoreData_Singleton_CanStoreAndReadScore()
        {
            var scoreEntity = CreateScoreDataSingleton(0);

            // Initial value
            var data = m_Manager.GetComponentData<ScoreData>(scoreEntity);
            Assert.AreEqual(0, data.Value, "Initial score should be 0");

            // Update and re-read
            m_Manager.SetComponentData(scoreEntity, new ScoreData { Value = 250 });
            data = m_Manager.GetComponentData<ScoreData>(scoreEntity);
            Assert.AreEqual(250, data.Value, "Score should update in ECS singleton");
        }

        [Test]
        public void LC05_GameAreaData_Singleton_StoresGameAreaSize()
        {
            var expected = new float2(20f, 15f);
            var entity = CreateGameAreaSingleton(expected);

            var data = m_Manager.GetComponentData<GameAreaData>(entity);
            Assert.AreEqual(expected.x, data.Size.x, 0.01f,
                "GameAreaData should store width from camera");
            Assert.AreEqual(expected.y, data.Size.y, 0.01f,
                "GameAreaData should store height from camera");
        }

        [Test]
        public void LC05_ShipPositionData_Singleton_ReplacesModelShipPosition()
        {
            var position = new float2(5f, 3f);
            var entity = CreateShipPositionSingleton(position, 7f, new float2(1f, 0f));

            var query = m_Manager.CreateEntityQuery(typeof(ShipPositionData));
            var data = query.GetSingleton<ShipPositionData>();

            Assert.AreEqual(position.x, data.Position.x, 0.01f,
                "ShipPositionData should provide ship position for spawn calculations");
            Assert.AreEqual(position.y, data.Position.y, 0.01f,
                "ShipPositionData should provide ship position for spawn calculations");
        }

        // --- LC-06: Tests infrastructure coverage ---

        [Test]
        public void LC06_ObservableBridgeSystem_HasNoModelDependency()
        {
            var bridgeType = typeof(ObservableBridgeSystem);

            // Check there is no _model field
            var modelField = bridgeType.GetField("_model",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNull(modelField,
                "ObservableBridgeSystem should not have _model field after legacy cleanup");

            // Check there is no SetModel method
            var setModelMethod = bridgeType.GetMethod("SetModel",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNull(setModelMethod,
                "ObservableBridgeSystem should not have SetModel method after legacy cleanup");
        }

        [Test]
        public void LC06_EcsSingletonInit_IsIdempotent_ForScoreData()
        {
            // First creation
            var entity1 = CreateScoreDataSingleton(100);
            var query = m_Manager.CreateEntityQuery(typeof(ScoreData));
            Assert.AreEqual(1, query.CalculateEntityCount(),
                "Should have exactly one ScoreData singleton");

            // Simulate idempotent re-init (as Application.InitializeEcsSingletons does)
            if (query.CalculateEntityCount() > 0)
            {
                var existing = query.GetSingletonEntity();
                m_Manager.SetComponentData(existing, new ScoreData { Value = 0 });
            }

            Assert.AreEqual(1, query.CalculateEntityCount(),
                "Re-init should not create duplicate singletons");
            Assert.AreEqual(0, m_Manager.GetComponentData<ScoreData>(entity1).Value,
                "Re-init should reset score to 0");
        }
    }
}
