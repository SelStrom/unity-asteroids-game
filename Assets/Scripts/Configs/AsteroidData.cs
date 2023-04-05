using UnityEngine;

namespace SelStrom.Asteroids.Configs
{
    [CreateAssetMenu(fileName = "AsteroidData", menuName = "Asteroid data", order = 1)]
    public class AsteroidData : ScriptableObject
    {
        public GameObject Prefab;
        [Space]
        public Sprite[] SpriteVariants;
    }
}