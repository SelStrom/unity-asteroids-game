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
            public GameObject EnemyPrefab;
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
            public GunData Gun;
        }

        [Serializable]
        public struct LaserData
        {
            public GameObject Prefab;
            public float BeamEffectLifetimeSec;
            public int LaserUpdateDurationSec;
            public int LaserMaxShoots;
        }

        [Serializable]
        public struct RocketData
        {
            public GameObject Prefab;
            public int MaxRockets;
            public float RespawnDurationSec;
            public float Speed;
            public float TurnRateDegPerSec;
            public float LifeTimeSec;
            public int Score;
        }

        public int AsteroidInitialCount;
        public int SpawnAllowedRadius;
        public float SpawnNewEnemyDurationSec;

        [Space]
        public GameObject VfxBlowPrefab;
        [Space] 
        public UfoData UfoBig;
        public UfoData Ufo;
        [Space]
        public AsteroidData AsteroidBig;
        public AsteroidData AsteroidMedium;
        public AsteroidData AsteroidSmall;
        [Space]
        public BulletData Bullet;
        public LaserData Laser;
        public RocketData Rocket;
        [Space]
        public ShipData Ship;

        [Space]
        [Header("Leaderboard")]
        public string LeaderboardId = "asteroids_highscores";
    }
}