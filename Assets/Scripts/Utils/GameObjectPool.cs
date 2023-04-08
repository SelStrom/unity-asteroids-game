using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SelStrom.Asteroids
{
    public class GameObjectPool
    {
        private readonly Dictionary<string, Stack<GameObject>> _prefabIdToGameObjects = new();
        private readonly Dictionary<GameObject, string> _gameObjectToPrefabId = new();
        
        private Transform _poolContainer;

        public void Connect(Transform poolContainer)
        {
            _poolContainer = poolContainer;
        }

        private GameObject Get(GameObject prefab, Transform parent)
        {
            GameObject gameObject;
            var prefabId = prefab.GetInstanceID().ToString();
            if (_prefabIdToGameObjects.TryGetValue(prefabId, out var gameObjects)
                && gameObjects.Count > 0)
            {
                gameObject = gameObjects.Pop();
                gameObject.transform.SetParent(parent, false);
                _gameObjectToPrefabId.Add(gameObject, prefabId);
                gameObject.SetActive(true);
            }
            else
            {
                gameObject = Object.Instantiate(prefab, parent);
                gameObject.name = prefab.name;
                _gameObjectToPrefabId.Add(gameObject, prefabId);
            }
            return gameObject;
        }

        public TComponent Get<TComponent>(GameObject prefab, Transform parent = null) where TComponent : Component
        {
            return Get(prefab, parent).GetComponent<TComponent>();
        }

        public void Release(GameObject gameObject)
        {
            if (!_gameObjectToPrefabId.TryGetValue(gameObject, out var prefabId))
            {
                throw new Exception($"[GameObjectPool] Unable to release GameObject '{gameObject.name}': prefab id not exists");
            }

            gameObject.SetActive(false);
            gameObject.transform.SetParent(_poolContainer, false);

            if (!_prefabIdToGameObjects.TryGetValue(prefabId, out var gameObjects))
            {
                gameObjects = new Stack<GameObject>();
                _prefabIdToGameObjects.Add(prefabId, gameObjects);
            }

            gameObjects.Push(gameObject);
            _gameObjectToPrefabId.Remove(gameObject);
        }

        public void CleanUp()
        {
            foreach (var (_, gameObjects) in _prefabIdToGameObjects)
            {
                while (gameObjects.Count > 0)
                {
                    var gameObject = gameObjects.Pop();
                    Object.Destroy(gameObject);
                }
            }

            _prefabIdToGameObjects.Clear();
            _gameObjectToPrefabId.Clear();
        }

        public void Dispose()
        {
            CleanUp();
            _poolContainer = null;
        }
    }
}