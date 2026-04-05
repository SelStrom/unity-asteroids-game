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
        public readonly ReactiveValue<string> RocketAmmoCount = new();
        public readonly ReactiveValue<string> RocketReloadTime = new();
        public readonly ReactiveValue<bool> IsRocketReloadTimeVisible = new();
    }

    public class HudVisual : AbstractWidgetView<HudData>
    {
        [SerializeField] private TMP_Text _coordinates = default;
        [SerializeField] private TMP_Text _rotationAngle = default;
        [SerializeField] private TMP_Text _speed = default;
        [SerializeField] private TMP_Text _laserShootCount = default;
        [SerializeField] private TMP_Text _laserReloadTime = default;
        [SerializeField] private TMP_Text _rocketAmmoCount = default;
        [SerializeField] private TMP_Text _rocketReloadTime = default;

        protected override void OnConnected()
        {
            Bind.From(ViewModel.Coordinates).To(_coordinates);
            Bind.From(ViewModel.RotationAngle).To(_rotationAngle);
            Bind.From(ViewModel.Speed).To(_speed);
            Bind.From(ViewModel.LaserShootCount).To(_laserShootCount);
            Bind.From(ViewModel.LaserReloadTime).To(_laserReloadTime);
            Bind.From(ViewModel.IsLaserReloadTimeVisible).To(_laserReloadTime.gameObject);

            if (_rocketAmmoCount == null || _rocketReloadTime == null)
            {
                Debug.LogWarning(
                    "[HudVisual] _rocketAmmoCount or _rocketReloadTime is not assigned in Inspector. " +
                    "Rocket HUD bindings skipped. Re-save the scene in Unity Editor.",
                    this);
                return;
            }

            Bind.From(ViewModel.RocketAmmoCount).To(_rocketAmmoCount);
            Bind.From(ViewModel.RocketReloadTime).To(_rocketReloadTime);
            Bind.From(ViewModel.IsRocketReloadTimeVisible).To(_rocketReloadTime.gameObject);
        }
    }
}