using System;
using Shtl.Mvvm;
using UnityEngine;

namespace Model.Components
{
    public class LaserComponent : IModelComponent
    {
        public Action<LaserComponent> OnShooting;
        
        public int MaxShoots;
        public float UpdateDurationSec;
        public ObservableValue<int> CurrentShoots = new();
        public ObservableValue<float> ReloadRemaining = new();
        
        public bool Shooting { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 ShootPosition { get; set; }
    }
}