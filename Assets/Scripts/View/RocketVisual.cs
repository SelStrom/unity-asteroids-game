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
        [SerializeField] private TrailRenderer _trail = default;

        protected override void OnConnected()
        {
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            // Сбросить накопленные сегменты следа при переиспользовании из пула,
            // иначе хвост от предыдущей ракеты протянется через весь экран.
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
