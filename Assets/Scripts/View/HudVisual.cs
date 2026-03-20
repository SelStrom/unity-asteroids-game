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
    }

    public class HudVisual : AbstractWidgetView<HudData>
    {
        [SerializeField] private TMP_Text _coordinates = default;
        [SerializeField] private TMP_Text _rotationAngle = default;
        [SerializeField] private TMP_Text _speed = default;
        [SerializeField] private TMP_Text _laserShootCount = default;
        [SerializeField] private TMP_Text _laserReloadTime = default;

        protected override void OnConnected()
        {
            Bind.From(ViewModel.Coordinates).To(_coordinates);
            Bind.From(ViewModel.RotationAngle).To(_rotationAngle);
            Bind.From(ViewModel.Speed).To(_speed);
            Bind.From(ViewModel.LaserShootCount).To(_laserShootCount);
            Bind.From(ViewModel.LaserReloadTime).To(_laserReloadTime);
            Bind.From(ViewModel.IsLaserReloadTimeVisible).To(_laserReloadTime.gameObject);
        }
    }
}