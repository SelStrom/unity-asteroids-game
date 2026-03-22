using System;
using SelStrom.Asteroids.Bindings;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ShipViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Vector2> Position = new();
        public readonly ReactiveValue<Vector2> Rotation = new();
        public readonly ReactiveValue<Sprite> Sprite = new();
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class ShipVisual : AbstractWidgetView<ShipViewModel>, IEntityView
    {
        [SerializeField] private SpriteRenderer _spriteRenderer = default;

        protected override void OnConnected()
        {
            Bind.From(ViewModel.Position).To(transform);
            ViewModel.Rotation.Connect(OnRotationChanged);
            ViewModel.Sprite.Connect(sprite => _spriteRenderer.sprite = sprite);
        }

        private void OnRotationChanged(Vector2 direction)
        {
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
