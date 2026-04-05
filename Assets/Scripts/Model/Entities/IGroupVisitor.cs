namespace SelStrom.Asteroids
{
    public interface IGroupVisitor
    {
        public void Visit(AsteroidModel model);
        public void Visit(BulletModel model);
        public void Visit(ShipModel model);
        public void Visit(UfoBigModel model);
        public void Visit(UfoModel model);
    }
}