using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SelStrom.Asteroids
{
    public class GameObjectPool
    {
        private readonly Dictionary<string, Stack<GameObject>> PrefabIdToGameObjects = new();
        private readonly Dictionary<GameObject, string> GameObjectToPrefabId = new();
        private Transform _poolContainer;

        public void Connect(Transform poolContainer)
        {
            _poolContainer = poolContainer;
        }

        private GameObject Get(GameObject prefab, Transform parent)
        {
            GameObject gameObject;
            var prefabId = prefab.GetInstanceID().ToString();
            if (PrefabIdToGameObjects.TryGetValue(prefabId, out var gameObjects)
                && gameObjects.Count > 0)
            {
                gameObject = gameObjects.Pop();
                gameObject.transform.SetParent(parent, false);
                GameObjectToPrefabId.Add(gameObject, prefabId);
                gameObject.SetActive(true);
            }
            else
            {
                gameObject = Object.Instantiate(prefab, parent);
                gameObject.name = prefab.name;
                GameObjectToPrefabId.Add(gameObject, prefabId);
            }
            return gameObject;
        }

        public TComponent Get<TComponent>(GameObject prefab, Transform parent = null) where TComponent : Component
        {
            return Get(prefab, parent).GetComponent<TComponent>();
        }

        public void Release(GameObject gameObject)
        {
            if (!GameObjectToPrefabId.TryGetValue(gameObject, out var prefabId))
            {
                throw new Exception($"[GameObjectPool] Unable to release GameObject '{gameObject.name}': prefab id not exists");
            }

            gameObject.SetActive(false);
            gameObject.transform.SetParent(_poolContainer, false);

            if (!PrefabIdToGameObjects.TryGetValue(prefabId, out var gameObjects))
            {
                gameObjects = new Stack<GameObject>();
                PrefabIdToGameObjects.Add(prefabId, gameObjects);
            }

            gameObjects.Push(gameObject);
            GameObjectToPrefabId.Remove(gameObject);
        }

        private void CleanUp()
        {
            foreach (var (_, gameObjects) in PrefabIdToGameObjects)
            {
                while (gameObjects.Count > 0)
                {
                    var gameObject = gameObjects.Pop();
                    Object.Destroy(gameObject);
                }
            }

            PrefabIdToGameObjects.Clear();
            GameObjectToPrefabId.Clear();
        }

        public void Dispose()
        {
            CleanUp();
            _poolContainer = null;
        }
    }
}