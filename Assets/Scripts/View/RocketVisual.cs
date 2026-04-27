using System;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class RocketViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class RocketVisual : AbstractWidgetView<RocketViewModel>, IEntityView
    {
        [SerializeField] private Collider2D _collider = default;
        [SerializeField] private ParticleSystem _trail = default;

        protected override void OnConnected()
        {
            _collider.enabled = true;
            if (_trail != null)
            {
                _trail.Play();
            }
        }

        protected override void OnDisposed()
        {
            if (_trail != null)
            {
                _trail.Stop();
                _trail.Clear();
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
