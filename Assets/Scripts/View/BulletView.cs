using UnityEngine;

namespace SelStrom.Asteroids
{
    public class BulletView : BaseView<BulletModel>
    {
        [SerializeField] private Transform _transform = default;

        protected override void OnConnected()
        {
            base.OnConnected();
            Data.Move.Position.OnChanged += OnPositionChanged;
            OnPositionChanged(Data.Move.Position.Value);
        }

        private void OnPositionChanged(Vector2 pos)
        {
            var position = _transform.position;
            position.x = pos.x;
            position.y = pos.y;
            _transform.position = position;
        }

        protected override void OnDisposed()
        {
            Data.Move.Position.OnChanged -= OnPositionChanged;
            base.OnDisposed();
        }
    }
}