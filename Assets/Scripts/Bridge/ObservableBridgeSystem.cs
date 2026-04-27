using System;
using System.Globalization;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ObservableBridgeSystem : SystemBase
    {
        private HudData _hudData;
        private ShipViewModel _shipViewModel;
        private int _laserMaxShoots;
        private int _missileMaxShoots;
        private Sprite _mainSprite;
        private Sprite _thrustSprite;

        public void SetHudData(HudData hudData)
        {
            _hudData = hudData;
        }

        public void SetShipViewModel(ShipViewModel viewModel, Sprite mainSprite, Sprite thrustSprite)
        {
            _shipViewModel = viewModel;
            _mainSprite = mainSprite;
            _thrustSprite = thrustSprite;
        }

        public void SetLaserMaxShoots(int maxShoots)
        {
            _laserMaxShoots = maxShoots;
        }

        public void SetMissileMaxShoots(int maxShoots)
        {
            _missileMaxShoots = maxShoots;
        }

        public void ClearReferences()
        {
            _hudData = null;
            _shipViewModel = null;
            _mainSprite = null;
            _thrustSprite = null;
        }

        protected override void OnUpdate()
        {
            if (_hudData == null && _shipViewModel == null)
            {
                return;
            }

            foreach (var (move, rotate, thrust, laser, launcher) in
                     SystemAPI.Query<RefRO<MoveData>, RefRO<RotateData>, RefRO<ThrustData>,
                             RefRO<LaserData>, RefRO<MissileLauncherData>>()
                         .WithAll<ShipTag>())
            {
                if (_hudData != null)
                {
                    var pos = move.ValueRO.Position;
                    _hudData.Coordinates.Value =
                        $"Coordinates: ({pos.x.ToString("F1", CultureInfo.InvariantCulture)}, {pos.y.ToString("F1", CultureInfo.InvariantCulture)})";

                    _hudData.Speed.Value =
                        $"Speed: {move.ValueRO.Speed.ToString("F1", CultureInfo.InvariantCulture)} points/sec";

                    var rot = rotate.ValueRO.Rotation;
                    var angle = math.atan2(rot.y, rot.x) * Mathf.Rad2Deg;
                    _hudData.RotationAngle.Value =
                        $"Rotation: {angle.ToString("F1", CultureInfo.InvariantCulture)} degrees";

                    var shoots = laser.ValueRO.CurrentShoots;
                    _hudData.LaserShootCount.Value = $"Laser shoots: {shoots.ToString()}";
                    _hudData.LaserReloadTime.Value =
                        $"Reload laser: {TimeSpan.FromSeconds((int)laser.ValueRO.ReloadRemaining):%s} sec";
                    _hudData.IsLaserReloadTimeVisible.Value = shoots < _laserMaxShoots;

                    var missiles = launcher.ValueRO.CurrentShoots;
                    _hudData.MissileShootCount.Value = $"Missiles: {missiles.ToString()}";
                    _hudData.MissileReloadTime.Value =
                        $"Reload missile: {TimeSpan.FromSeconds((int)launcher.ValueRO.ReloadRemaining):%s} sec";
                    _hudData.IsMissileReloadTimeVisible.Value = missiles < _missileMaxShoots;
                }

                if (_shipViewModel != null)
                {
                    if (_mainSprite != null && _thrustSprite != null)
                    {
                        _shipViewModel.Sprite.Value =
                            thrust.ValueRO.IsActive ? _thrustSprite : _mainSprite;
                    }
                }
            }
        }
    }
}
