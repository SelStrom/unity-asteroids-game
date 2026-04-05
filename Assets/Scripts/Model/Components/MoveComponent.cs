using Shtl.Mvvm;
using UnityEngine;

namespace Model.Components
{
    public class MoveComponent : IModelComponent    
    {
        public readonly ObservableValue<Vector2> Position = new();
        public readonly ObservableValue<float> Speed = new();
        public Vector2 Direction { get; set; }
    }
}