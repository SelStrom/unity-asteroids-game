using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ShipModel : IGameEntityModel
    {
        public readonly RotateComponent Rotate = new();
        public readonly ThrustComponent Thrust = new();
        public readonly MoveComponent Move = new();
        public readonly GunComponent Gun = new();
        public readonly LaserComponent Laser = new();

        public GameData.ShipData Data { get; private set; }
        
        public Vector2 ShootPoint => Move.Position.Value + Rotate.Rotation.Value;

        private bool _killed;
        public bool IsDead() => _killed;

        public void SetData(GameData.ShipData data)
        {
            Data = data;
        }

        public void AcceptWith(IGroupVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void Kill()
        {
            _killed = true;            
        }
    }
}