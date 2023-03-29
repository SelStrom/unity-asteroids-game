using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ShipView : BaseView<ShipModel>
    {
        [SerializeField] private Transform _transform = default;
        
        protected override void OnConnected()
        {
            base.OnConnected();
            
            Data.Move.OnPositionChanged += OnPositionChanged;
            Data.OnRotationChanged += OnRotationChanged;
            
            OnPositionChanged();
            OnRotationChanged();
        }

        public void OnRotationChanged()
        {
            var angle = Mathf.Atan2(Data.Rotation.y, Data.Rotation.x) * Mathf.Rad2Deg;
            _transform.rotation = Quaternion.Euler(new Vector3(0,0, angle));
            
            // _transform.rotation = Quaternion.LookRotation( Vector3.forward, new Vector3(Data.Direction.y, Data.Direction.x)); #1
            
            // var rotation = Quaternion.LookRotation( Vector3.forward, Data.Direction ); //#2
            // transform.rotation = Quaternion.Euler(0,0, rotation.eulerAngles.z + 90);
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
            Data.OnRotationChanged -= OnRotationChanged;
            
            base.OnDisposed();
        }
    }
}