using System;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class UfoViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class UfoVisual : AbstractWidgetView<UfoViewModel>, IEntityView
    {
        protected override void OnConnected()
        {
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
