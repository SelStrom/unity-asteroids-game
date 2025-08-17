using UnityEngine;

namespace SelStrom.Asteroids
{
    public sealed class HudData
    {
        public readonly ObservableField<string> Coordinates = new();
        public readonly ObservableField<string> RotationAngle = new();
        public readonly ObservableField<string> Speed = new();
        public readonly ObservableField<string> LaserShootCount = new();
        public readonly ObservableField<string> LaserReloadTime = new();
        public readonly ObservableField<bool> LaserReloadTimeVisible = new();
    }

    public class HudVisual : BaseVisual<HudData>
    {
        [SerializeField] private GuiText _coordinates = default;
        [SerializeField] private GuiText _rotationAngle = default;
        [SerializeField] private GuiText _speed = default;
        [SerializeField] private GuiText _laserShootCount = default;
        [SerializeField] private GuiText _laserReloadTime = default;

        protected override void OnConnected()
        {
            _coordinates.Connect(Data.Coordinates);
            _rotationAngle.Connect(Data.RotationAngle);
            _speed.Connect(Data.Speed);
            _laserShootCount.Connect(Data.LaserShootCount);
            _laserReloadTime.Connect(Data.LaserReloadTime);
            
            Data.LaserReloadTimeVisible.OnChanged += OnLaserReloadTimeVisibleChanged;
            OnLaserReloadTimeVisibleChanged(Data.LaserReloadTimeVisible.Value);
        }

        private void OnLaserReloadTimeVisibleChanged(bool isVisible)
        {
            _laserReloadTime.gameObject.SetActive(isVisible);
        }

        protected override void OnDisposed()
        {
            _coordinates.Dispose();
            _rotationAngle.Dispose();
            _speed.Dispose();
            _laserShootCount.Dispose();
            _laserReloadTime.Dispose();

            Data.LaserReloadTimeVisible.OnChanged -= OnLaserReloadTimeVisibleChanged;

            base.OnDisposed();
        }
    }
}