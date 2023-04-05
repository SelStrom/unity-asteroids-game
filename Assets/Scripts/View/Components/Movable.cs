using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Movable : BaseVisual<ObservableField<Vector2>>
    {
        [SerializeField] private Transform _transform = default;

        protected override void OnConnected()
        {
            Data.OnChanged += OnChanged;
            OnChanged(Data.Value);
        }
        
        private void OnChanged(Vector2 pos)
        {
            var position = _transform.position;
            position.x = pos.x;
            position.y = pos.y;
            _transform.position = position;
        }

        protected override void OnDisposed()
        {
            Data.OnChanged -= OnChanged;
            base.OnDisposed();
        }
    }
}