namespace Model.Components
{
    public interface IModelComponent
    {
        void Update(float deltaTime);
        void Connect(SelStrom.Asteroids.Model model);
    }
}