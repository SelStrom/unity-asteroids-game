using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class BulletView : BaseView<(BulletModel BulletModel, GameController GameController)>
    {
        [SerializeField] private Transform _transform = default;

        protected override void OnConnected()
        {
            base.OnConnected();
            Data.BulletModel.Move.Position.OnChanged += OnPositionChanged;
            OnPositionChanged(Data.BulletModel.Move.Position.Value);
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
            Data.BulletModel.Move.Position.OnChanged -= OnPositionChanged;
            base.OnDisposed();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            Data.GameController.KillBullet(Data.BulletModel);
            Data.GameController.KillAsteroid(col.gameObject);
        }
    }
}