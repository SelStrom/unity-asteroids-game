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
            
            // _transform.rotation = Quaternion.LookRotation( Vector3.forward, new Vector3(Data.Direction.y, Data.Direction.x)); #1
            
            // var rotation = Quaternion.LookRotation( Vector3.forward, Data.Direction ); //#2
            // transform.rotation = Quaternion.Euler(0,0, rotation.eulerAngles.z + 90);
        }

        protected override void OnDisposed()
        {
            Data.OnChanged -= OnChanged;
            base.OnDisposed();
        }
    }
}