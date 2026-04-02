using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids.ECS
{
    public class GameObjectRef : ICleanupComponentData
    {
        public Transform Transform;
        public GameObject GameObject;
    }
}
