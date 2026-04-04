using System;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class AsteroidViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Sprite> Sprite = new();
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class AsteroidVisual : AbstractWidgetView<AsteroidViewModel>, IEntityView
    {
        [SerializeField] private SpriteRenderer _spriteRenderer = default;

        protected override void OnConnected()
        {
            ViewModel.Sprite.Connect(sprite => _spriteRenderer.sprite = sprite);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
