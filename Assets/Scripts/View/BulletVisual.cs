using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public struct BulletVisualData
    {
        public BulletModel BulletModel;
        public Action<BulletModel, Collision2D> OnRegisterCollision;
    }
    
    public class BulletVisual : BaseVisual<BulletVisualData>
    {
        [SerializeField] private Movable _movable = default;
        [SerializeField] private Collider2D _collider = default;

        protected override void OnConnected()
        {
            _collider.enabled = true;
            _movable.Connect(Data.BulletModel.Move.Position);
        }

        protected override void OnDisposed()
        {
            _movable.Dispose();
            base.OnDisposed();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            Data.OnRegisterCollision?.Invoke(Data.BulletModel, col);
        }
    }
}