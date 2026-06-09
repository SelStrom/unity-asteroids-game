using System;
using Shtl.Mvvm;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class RocketViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<Sprite> Sprite = new();
        public readonly ReactiveValue<Action<Collision2D>> OnCollision = new();
    }

    public class RocketVisual : AbstractWidgetView<RocketViewModel>, IEntityView
    {
        [SerializeField] private SpriteRenderer _spriteRenderer = default;
        [SerializeField] private Collider2D _collider = default;
        [SerializeField] private ParticleSystem _trail = default;

        protected override void OnConnected()
        {
            _collider.enabled = true;
            ViewModel.Sprite.Connect(sprite => _spriteRenderer.sprite = sprite);
            _trail.Play();
        }

        protected override void OnDisposed()
        {
            _trail.Stop();
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ViewModel.OnCollision.Value?.Invoke(col);
        }
    }
}
