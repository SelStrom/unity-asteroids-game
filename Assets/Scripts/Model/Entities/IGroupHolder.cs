namespace SelStrom.Asteroids
{
    public interface IGroupHolder
    {
        public void Group(AsteroidModel model);
        public void Group(BulletModel model);
        public void Group(ShipModel model);
    }
}