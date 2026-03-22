using System;
using SelStrom.Asteroids.Bindings;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class BulletViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Vector2> Position = new();
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class BulletVisual : AbstractWidgetView<BulletViewModel>, IEntityView
    {
        [SerializeField] private Collider2D _collider = default;

        protected override void OnConnected()
        {
            _collider.enabled = true;
            Bind.From(ViewModel.Position).To(transform);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
