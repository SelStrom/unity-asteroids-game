using System;
using SelStrom.Asteroids.Bindings;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class UfoViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Vector2> Position = new();
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class UfoVisual : AbstractWidgetView<UfoViewModel>, IEntityView
    {
        protected override void OnConnected()
        {
            Bind.From(ViewModel.Position).To(transform);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
