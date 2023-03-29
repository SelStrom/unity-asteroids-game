using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class BulletView : BaseView<BulletModel>
    {
        [SerializeField] private Transform _transform = default;

        protected override void OnConnected()
        {
            base.OnConnected();
            Data.Move.OnPositionChanged += OnPositionChanged;
            OnPositionChanged();
        }

        public void OnPositionChanged()
        {
            var position = _transform.position;
            position.x = Data.Move.Position.x;
            position.y = Data.Move.Position.y;
            _transform.position = position;
        }

        protected override void OnDisposed()
        {
            Data.Move.OnPositionChanged -= OnPositionChanged;
            base.OnDisposed();
        }
    }
}