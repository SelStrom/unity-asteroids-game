using System;
using UnityEngine;

namespace SelStrom.Asteroids.Configs
{
    [CreateAssetMenu(fileName = "BulletData", menuName = "Bullet data", order = 1)]
    public class BulletData: ScriptableObject
    {
        public GameObject Prefab;
        public int LifeTimeSeconds;
        public float Speed;
    }
}