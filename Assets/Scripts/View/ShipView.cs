using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ShipView : BaseView<(ShipModel ShipModel, GameController GameController)>
    {
        [SerializeField] private Transform _transform = default;
        
        protected override void OnConnected()
        {
            base.OnConnected();
            
            Data.ShipModel.Move.Position.OnChanged += OnPositionChanged;
            Data.ShipModel.Rotation.OnChanged += OnRotationChanged;
            
            OnPositionChanged(Data.ShipModel.Move.Position.Value);
            OnRotationChanged(Data.ShipModel.Rotation.Value);
        }

        private void OnRotationChanged(Vector2 direction)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _transform.rotation = Quaternion.Euler(new Vector3(0,0, angle));
            
            // _transform.rotation = Quaternion.LookRotation( Vector3.forward, new Vector3(Data.Direction.y, Data.Direction.x)); #1
            
            // var rotation = Quaternion.LookRotation( Vector3.forward, Data.Direction ); //#2
            // transform.rotation = Quaternion.Euler(0,0, rotation.eulerAngles.z + 90);
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
            Data.ShipModel.Move.Position.OnChanged -= OnPositionChanged;
            Data.ShipModel.Rotation.OnChanged -= OnRotationChanged;
            
            base.OnDisposed();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            Data.GameController.Kill(Data.ShipModel);
        }
    }
}