using System.Collections.Generic;

namespace SelStrom.Asteroids
{
    public interface IModelSystem
    {
        public void Update(float deltaTime);
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

        protected abstract void UpdateNode(TNode com, float deltaTime);
    }
}