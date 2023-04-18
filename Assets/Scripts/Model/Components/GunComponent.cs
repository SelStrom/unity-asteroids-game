using System;
using UnityEngine;

namespace Model.Components
{
    public class GunComponent : IModelComponent
    {
        public Action<GunComponent> OnShooting;
        
        public int MaxShoots;
        public float ReloadDurationSec;
        public int CurrentShoots { get; set; }
        public float ReloadRemaining { get; set; }
        
        public bool Shooting { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 ShootPosition { get; set; }
    }
}