using System;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class MissileViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class MissileVisual : AbstractWidgetView<MissileViewModel>, IEntityView
    {
        [SerializeField] private Collider2D _collider = default;
        private TrailRenderer _trail;

        protected override void OnConnected()
        {
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_trail == null)
            {
                _trail = GetComponentInChildren<TrailRenderer>();
            }

            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = true;
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
