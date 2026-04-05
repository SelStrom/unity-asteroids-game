using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public sealed class UfoModel : UfoBigModel
    {
        public readonly MoveToComponent MoveTo = new();
        
        public override void AcceptWith(IGroupVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    public class UfoBigModel : IGameEntityModel
    {
        public readonly MoveComponent Move = new();
        public readonly ShootToComponent ShootTo = new();
        public readonly GunComponent Gun = new();
        
        public UfoData Data { get; private set; }
        
        private bool _killed;
        public bool IsDead() => _killed;
        
        public void SetData(UfoData data, Vector2 position, Vector2 direction, float speed)
        {
            Data = data;
            Move.Position.Value = position;
            Move.Direction = direction;
            Move.Speed.Value = speed;
        }

        public virtual void AcceptWith(IGroupVisitor visitor)
        {
            visitor.Visit(this);
        }

        public void Kill()
        {
            _killed = true;            
        }
    }
    
    
}