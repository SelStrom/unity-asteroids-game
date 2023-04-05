using System;
using UnityEngine;

namespace SelStrom.Asteroids.Configs
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Game data", order = 0)]
    public class GameData : ScriptableObject
    {
        [Serializable]
        public struct BulletData
        {
            public GameObject Prefab;
            public int LifeTimeSeconds;
            public float Speed;
        }

        public int AsteroidInitialCount;
        public int AsteroidSpawnAllowedRadius;
        public int SpawnNewEnemyDurationSec;

        [Space]
        public GameObject ShipPrefab = default;
        public GameObject UfoMediumPrefab = default;
        public GameObject UfoSmallPrefab = default;
        [Space]
        public AsteroidData AsteroidBig;
        public AsteroidData AsteroidMedium;
        public AsteroidData AsteroidSmall;
        [Space]
        public BulletData Bullet;

    }
}