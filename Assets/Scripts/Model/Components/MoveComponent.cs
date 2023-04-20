using SelStrom.Asteroids;
using UnityEngine;

namespace Model.Components
{
    public class MoveComponent : IModelComponent    
    {
        public readonly ObservableField<Vector2> Position = new();
        public readonly ObservableField<float> Speed = new();
        public Vector2 Direction { get; set; }
    }
}