using System;

namespace SelStrom.Asteroids
{
    public interface IGameEntityModel
    {
        bool IsDead();
        void Kill();
        
        public void ConnectWith(IGroupHolder groupHolder);
    }
}