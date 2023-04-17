namespace SelStrom.Asteroids
{
    public interface IGameEntityModel
    {
        bool IsDead();
        void Kill();

        public void AcceptWith(IGroupVisitor visitor);
    }
}