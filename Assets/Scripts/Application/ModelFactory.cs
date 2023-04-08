namespace SelStrom.Asteroids
{
    public class ModelFactory
    {
        private readonly Model _model;

        public ModelFactory(Model model)
        {
            _model = model;
        }

        public TModel Get<TModel>() where TModel : class, IGameEntityModel, new()
        {
            // TODO @a.shatalov: model pool

            var model = new TModel();
            _model.AddEntity(model);
            return model;
        }

        public void Release(IGameEntityModel model)
        {
            // TODO @a.shatalov: model pool
        }
    }
}