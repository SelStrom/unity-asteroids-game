using System;
using UnityEngine;

namespace Model.Components
{
    public class LaserComponent : IModelComponent
    {
        public Action<LaserComponent> OnShooting;
        
        public int MaxShoots;
        public float UpdateDurationSec;
        public int CurrentShoots { get; set; }
        public float ReloadRemaining { get; set; }
        
        public bool Shooting { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 ShootPosition { get; set; }
    }
}