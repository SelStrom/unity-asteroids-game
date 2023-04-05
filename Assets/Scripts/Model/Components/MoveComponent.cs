using System;
using SelStrom.Asteroids;
using UnityEngine;

namespace Model.Components
{
    public class MoveComponent : IModelComponent    
    {
        public readonly ObservableField<Vector2> Position = new();
        public float Speed { get; set; }
        public Vector2 Direction { get; set; }
    }
}