using UnityEngine;

namespace SelStrom.Asteroids.Configs
{
    [CreateAssetMenu(fileName = "GunData", menuName = "Gun data", order = 1)]
    public class GunData : ScriptableObject
    {
        public int MaxShoots;
        public float ReloadDurationSec;
    }
}