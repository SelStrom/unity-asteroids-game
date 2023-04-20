using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public struct UfoVisualData
    {
        public UfoBigModel UfoModel;
        public Action<UfoBigModel> OnRegisterCollision;
    }

    public class UfoVisual : BaseVisual<UfoVisualData>
    {
        [SerializeField] private Movable _movable = default;

        protected override void OnConnected()
        {
            _movable.Connect(Data.UfoModel.Move.Position);
        }

        protected override void OnDisposed()
        {
            _movable.Dispose();
            base.OnDisposed();
        }       
        
        private void OnCollisionEnter2D(Collision2D col)
        {
            Data.OnRegisterCollision?.Invoke(Data.UfoModel);
        }
    }
}