using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class EffectVisual : BaseVisual<Action<EffectVisual>>
    {
        [SerializeField] private ParticleSystem _particleSystem = default;
        
        protected override void OnConnected()
        {
            _particleSystem.Play();
        }

        private void OnParticleSystemStopped()
        {
            Data?.Invoke(this);
        }
    }
}