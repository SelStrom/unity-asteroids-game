using Shtl.Mvvm;
using TMPro;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public sealed class HudData : AbstractViewModel
    {
        public readonly ReactiveValue<string> Coordinates = new();
        public readonly ReactiveValue<string> RotationAngle = new();
        public readonly ReactiveValue<string> Speed = new();
        public readonly ReactiveValue<string> LaserShootCount = new();
        public readonly ReactiveValue<string> LaserReloadTime = new();
        public readonly ReactiveValue<bool> IsLaserReloadTimeVisible = new();
        public readonly ReactiveValue<string> RocketCount = new();
        public readonly ReactiveValue<string> RocketRespawnTime = new();
        public readonly ReactiveValue<bool> IsRocketRespawnVisible = new();
    }

    public class HudVisual : AbstractWidgetView<HudData>
    {
        [SerializeField] private TMP_Text _coordinates = default;
        [SerializeField] private TMP_Text _rotationAngle = default;
        [SerializeField] private TMP_Text _speed = default;
        [SerializeField] private TMP_Text _laserShootCount = default;
        [SerializeField] private TMP_Text _laserReloadTime = default;
        [SerializeField] private TMP_Text _rocketCount = default;
        [SerializeField] private TMP_Text _rocketRespawnTime = default;

        protected override void OnConnected()
        {
            Bind.From(ViewModel.Coordinates).To(_coordinates);
            Bind.From(ViewModel.RotationAngle).To(_rotationAngle);
            Bind.From(ViewModel.Speed).To(_speed);
            Bind.From(ViewModel.LaserShootCount).To(_laserShootCount);
            Bind.From(ViewModel.LaserReloadTime).To(_laserReloadTime);
            Bind.From(ViewModel.IsLaserReloadTimeVisible).To(_laserReloadTime.gameObject);
            if (_rocketCount != null)
            {
                Bind.From(ViewModel.RocketCount).To(_rocketCount);
            }
            if (_rocketRespawnTime != null)
            {
                Bind.From(ViewModel.RocketRespawnTime).To(_rocketRespawnTime);
                Bind.From(ViewModel.IsRocketRespawnVisible).To(_rocketRespawnTime.gameObject);
            }
        }
    }
}