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
            public int LifeTimeSeconds;
            public float Speed;
        }

        public GameObject ShipPrefab = default;
        public GameObject BulletPrefab = default;

        public GameObject AsteroidBigPrefab = default;
        public GameObject AsteroidMediumPrefab = default;
        public GameObject AsteroidSmallPrefab = default;

        public GameObject UfoMediumPrefab = default;
        public GameObject UfoSmallPrefab = default;

        public BulletData Bullet;

        public int AsteroidSpawnAllowedRadius;
    }
}