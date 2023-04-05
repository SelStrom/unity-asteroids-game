using System;
using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ShipModel : IGameEntityModel
    {
        public MoveComponent Move = new();
        public RotateComponent Rotate = new();
        public ThrustComponent Thrust = new();
        
        private bool _killed;

        public bool IsDead() => _killed;
        
        public void Kill()
        {
            _killed = true;            
        }

        public void ConnectWith(IGroupHolder groupHolder)
        {
            groupHolder.Group(this);
        }
    }
}