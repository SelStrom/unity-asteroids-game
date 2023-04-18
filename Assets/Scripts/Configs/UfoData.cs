using UnityEngine;

namespace SelStrom.Asteroids.Configs
{
    [CreateAssetMenu(fileName = "UfoData", menuName = "Ufo data", order = 1)]
    public class UfoData : BaseGameEntityData
    {
        public GameObject Prefab;
        public float Speed;
        [Space] 
        public GunData Gun;
    }
}