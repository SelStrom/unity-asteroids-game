using System;

namespace SelStrom.Asteroids
{
    public interface IApplicationComponent
    {
        public event Action<float> OnUpdate;
        public event Action OnPause;
        public event Action OnResume;
    }
}