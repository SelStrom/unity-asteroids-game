using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class AsteroidVisual : BaseVisual<(ObservableField<Vector2> Position, Sprite[] SpriteVariants)>
    {
        [SerializeField] private SpriteRenderer _spriteRenderer = default;
        [SerializeField] private Movable _movable = default;

        protected override void OnConnected()
        {
            _spriteRenderer.sprite = Data.SpriteVariants[Random.Range(0, Data.SpriteVariants.Length)];
            _movable.Connect(Data.Position);
        }
        protected override void OnDisposed()
        {
            _movable.Dispose();
            base.OnDisposed();
        }
    }
}