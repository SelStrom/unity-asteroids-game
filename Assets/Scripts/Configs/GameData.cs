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

        [Serializable]
        public struct ShipData
        {
            public GameObject Prefab;
            [Space]     
            public Sprite MainSprite;
            public Sprite ThrustSprite;
            [Space] 
            public float ThrustUnitsPerSecond;
            public float MaxSpeed;
            [Space] 
            public int LaserUpdateDurationSec;
            public int LaserMaxShoots;
        }

        public int AsteroidInitialCount;
        public int AsteroidSpawnAllowedRadius;
        public int SpawnNewEnemyDurationSec;

        [Space]
        public GameObject UfoMediumPrefab = default;
        public GameObject UfoSmallPrefab = default;
        [Space]
        public AsteroidData AsteroidBig;
        public AsteroidData AsteroidMedium;
        public AsteroidData AsteroidSmall;
        [Space]
        public BulletData Bullet;
        [Space]
        public ShipData Ship;

    }
}