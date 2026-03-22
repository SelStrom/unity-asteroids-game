using SelStrom.Asteroids.Bindings;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class AsteroidViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Vector2> Position = new();
        public readonly ReactiveValue<Sprite> Sprite = new();
    }

    public class AsteroidVisual : AbstractWidgetView<AsteroidViewModel>, IEntityView
    {
        [SerializeField] private SpriteRenderer _spriteRenderer = default;

        protected override void OnConnected()
        {
            Bind.From(ViewModel.Position).To(transform);
            ViewModel.Sprite.Connect(sprite => _spriteRenderer.sprite = sprite);
        }
    }
}
