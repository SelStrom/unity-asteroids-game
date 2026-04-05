using System.Collections.Generic;

namespace SelStrom.Asteroids
{
    public interface IModelSystem
    {
        public void Update(float deltaTime);
        public void Remove(IGameEntityModel model);
        public void CleanUp();
    }

    public abstract class BaseModelSystem<TNode> : IModelSystem
    {
        private readonly Dictionary<IGameEntityModel, TNode> _entityToNode = new();

        public void Add(IGameEntityModel model, TNode node)
        {
            _entityToNode.Add(model, node);
        }

        public void Remove(IGameEntityModel model)
        {
            _entityToNode.Remove(model);
        }

        public void Update(float deltaTime)
        {
            foreach (var node in _entityToNode.Values)
            {
                UpdateNode(node, deltaTime);
            }
        }

        public void CleanUp()
        {
            _entityToNode.Clear();
        }

        protected abstract void UpdateNode(TNode node, float deltaTime);
    }
}