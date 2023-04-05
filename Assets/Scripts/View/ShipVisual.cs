using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public struct ShipVisualData
    {
        public ShipModel ShipModel;
        public Action<Collision2D> OnRegisterCollision;
    }
    
    public class ShipVisual : BaseVisual<ShipVisualData>
    {
        [SerializeField] private Movable _movable = default;
        [SerializeField] private Rotatable _rotatable = default;

        protected override void OnConnected()
        {
            base.OnConnected();
            
            _movable.Connect(Data.ShipModel.Move.Position);
            _rotatable.Connect(Data.ShipModel.Rotate.Rotation);
        }
        
        protected override void OnDisposed()
        {
            _rotatable.Dispose();
            _movable.Dispose();

            base.OnDisposed();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            Data.OnRegisterCollision?.Invoke(col);
        }
    }
}