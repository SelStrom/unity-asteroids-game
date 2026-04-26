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
        [SerializeField] private ParticleSystem _trail = default;

        protected override void OnConnected()
        {
            _collider.enabled = true;
            if (_trail != null)
            {
                _trail.Clear(true);
                _trail.Play(true);
            }
        }

        private void OnDisable()
        {
            if (_trail != null)
            {
                _trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
