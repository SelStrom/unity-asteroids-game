using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ViewFactory
    {
        private readonly GameObjectPool _gameObjectPool;
        private readonly Transform _gameContainer;

        public ViewFactory(GameObjectPool gameObjectPool, Transform gameContainer)
        {
            _gameObjectPool = gameObjectPool;
            _gameContainer = gameContainer;
        }

        public TView Get<TView>(GameObject prefab) where TView : Component
        {
            return _gameObjectPool.Get<TView>(prefab, _gameContainer);
        }

        public void Release(BaseVisual view)
        {
            view.Dispose();
            _gameObjectPool.Release(view.gameObject);
        }
        
        public void Release(GameObject gameObject)
        {
            _gameObjectPool.Release(gameObject);
        }
    }
}