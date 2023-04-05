using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Rotatable : BaseVisual<ObservableField<Vector2>>
    {
        [SerializeField] private Transform _transform = default;

        protected override void OnConnected()
        {
            Data.OnChanged += OnChanged;
            OnChanged(Data.Value);
        }
        
        private void OnChanged(Vector2 direction)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _transform.rotation = Quaternion.Euler(new Vector3(0,0, angle));
        }

        protected override void OnDisposed()
        {
            Data.OnChanged -= OnChanged;
            base.OnDisposed();
        }
    }
}