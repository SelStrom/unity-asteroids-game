using System;

namespace SelStrom.Asteroids
{
    public interface IGameEntityModel
    {
        void Update(float deltaTime);
        void Connect(Model model);
        bool IsDead();
        void Kill();
    }
}