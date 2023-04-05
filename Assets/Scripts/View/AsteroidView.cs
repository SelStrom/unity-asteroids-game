using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class AsteroidView : BaseView<AsteroidModel>
    {
        [SerializeField] private Transform _transform = default;
        [SerializeField] private SpriteRenderer _spriteRenderer = default;
        [SerializeField] private List<Sprite> _spriteVariants = default;

        protected override void OnConnected()
        {
            base.OnConnected();

            _spriteRenderer.sprite = _spriteVariants[Random.Range(0, _spriteVariants.Count)];
            
            Data.Move.Position.OnChanged += OnPositionChanged;
            OnPositionChanged(Data.Move.Position.Value);
        }

        private void OnPositionChanged(Vector2 pos)
        {
            var position = _transform.position;
            position.x = pos.x;
            position.y = pos.y;
            _transform.position = position;
        }

        protected override void OnDisposed()
        {
            Data.Move.Position.OnChanged -= OnPositionChanged;
            base.OnDisposed();
        }
    }
}