using NUnit.Framework;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids.Tests.EditMode.ECS
{
    public class ObservableBridgeSystemTests : AsteroidsEcsTestFixture
    {
        private ObservableBridgeSystem _system;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.AddSystemManaged(new ObservableBridgeSystem());
        }

        [TearDown]
        public override void TearDown()
        {
            _system.ClearReferences();
            base.TearDown();
        }

        private Entity CreateFullShipEntity(
            float2 position, float speed, float2 direction,
            float2 rotation, bool thrustActive,
            int laserCurrentShoots, int laserMaxShoots, float reloadRemaining)
        {
            var entity = CreateShipEntity(position, speed);
            m_Manager.SetComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            m_Manager.SetComponentData(entity, new RotateData
            {
                Rotation = rotation
            });
            m_Manager.SetComponentData(entity, new ThrustData
            {
                IsActive = thrustActive
            });
            m_Manager.SetComponentData(entity, new LaserData
            {
                CurrentShoots = laserCurrentShoots,
                MaxShoots = laserMaxShoots,
                ReloadRemaining = reloadRemaining
            });
            return entity;
        }

        [Test]
        public void PushesCoordinates_ToHudData()
        {
            var hudData = new HudData();
            _system.SetHudData(hudData);

            CreateFullShipEntity(
                position: new float2(5.5f, 3.2f),
                speed: 0f,
                direction: float2.zero,
                rotation: new float2(1f, 0f),
                thrustActive: false,
                laserCurrentShoots: 3,
                laserMaxShoots: 3,
                reloadRemaining: 0f);

            _system.Update();

            Assert.IsTrue(hudData.Coordinates.Value.Contains("5.5"),
                "Coordinates should contain x=5.5");
            Assert.IsTrue(hudData.Coordinates.Value.Contains("3.2"),
                "Coordinates should contain y=3.2");
        }

        [Test]
        public void PushesSpeed_ToHudData()
        {
            var hudData = new HudData();
            _system.SetHudData(hudData);

            CreateFullShipEntity(
                position: float2.zero,
                speed: 7.8f,
                direction: float2.zero,
                rotation: new float2(1f, 0f),
                thrustActive: false,
                laserCurrentShoots: 3,
                laserMaxShoots: 3,
                reloadRemaining: 0f);

            _system.Update();

            Assert.IsTrue(hudData.Speed.Value.Contains("7.8"),
                "Speed should contain value 7.8");
            Assert.IsTrue(hudData.Speed.Value.Contains("points/sec"),
                "Speed should contain units");
        }

        [Test]
        public void PushesRotation_ToHudData()
        {
            var hudData = new HudData();
            _system.SetHudData(hudData);

            CreateFullShipEntity(
                position: float2.zero,
                speed: 0f,
                direction: float2.zero,
                rotation: new float2(0f, 1f),
                thrustActive: false,
                laserCurrentShoots: 3,
                laserMaxShoots: 3,
                reloadRemaining: 0f);

            _system.Update();

            Assert.IsTrue(hudData.RotationAngle.Value.Contains("90.0"),
                "Rotation should be 90.0 degrees for (0,1)");
            Assert.IsTrue(hudData.RotationAngle.Value.Contains("degrees"),
                "Rotation should contain units");
        }

        [Test]
        public void PushesLaserData_ToHudData()
        {
            var hudData = new HudData();
            _system.SetHudData(hudData);
            _system.SetLaserMaxShoots(3);

            CreateFullShipEntity(
                position: float2.zero,
                speed: 0f,
                direction: float2.zero,
                rotation: new float2(1f, 0f),
                thrustActive: false,
                laserCurrentShoots: 2,
                laserMaxShoots: 3,
                reloadRemaining: 3.0f);

            _system.Update();

            Assert.AreEqual("Laser shoots: 2", hudData.LaserShootCount.Value,
                "LaserShootCount should show current shoots");
            Assert.IsTrue(hudData.IsLaserReloadTimeVisible.Value,
                "IsLaserReloadTimeVisible should be true when shoots < maxShoots");
        }

        [Test]
        public void PushesPosition_ToShipViewModel()
        {
            var shipViewModel = new ShipViewModel();
            _system.SetShipViewModel(shipViewModel, null, null);

            CreateFullShipEntity(
                position: new float2(1f, 2f),
                speed: 0f,
                direction: float2.zero,
                rotation: new float2(1f, 0f),
                thrustActive: false,
                laserCurrentShoots: 3,
                laserMaxShoots: 3,
                reloadRemaining: 0f);

            _system.Update();

            Assert.AreEqual(new Vector2(1f, 2f), shipViewModel.Position.Value,
                "ShipViewModel.Position should match ECS MoveData.Position");
        }

        [Test]
        public void PushesRotation_ToShipViewModel()
        {
            var shipViewModel = new ShipViewModel();
            _system.SetShipViewModel(shipViewModel, null, null);

            CreateFullShipEntity(
                position: float2.zero,
                speed: 0f,
                direction: float2.zero,
                rotation: new float2(0.707f, 0.707f),
                thrustActive: false,
                laserCurrentShoots: 3,
                laserMaxShoots: 3,
                reloadRemaining: 0f);

            _system.Update();

            Assert.AreEqual(new Vector2(0.707f, 0.707f), shipViewModel.Rotation.Value,
                "ShipViewModel.Rotation should match ECS RotateData.Rotation");
        }

        [Test]
        public void DoesNotCrash_WhenNoHudDataSet()
        {
            CreateFullShipEntity(
                position: float2.zero,
                speed: 0f,
                direction: float2.zero,
                rotation: new float2(1f, 0f),
                thrustActive: false,
                laserCurrentShoots: 3,
                laserMaxShoots: 3,
                reloadRemaining: 0f);

            Assert.DoesNotThrow(() => _system.Update(),
                "System should not crash when no HudData or ShipViewModel is set");
        }
    }
}
