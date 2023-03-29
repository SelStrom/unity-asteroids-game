namespace SelStrom.Asteroids
{
    public interface IGameEntity
    {
        void Update(float deltaTime);
        void Connect(Model model);
        bool IsDead();
    }
}