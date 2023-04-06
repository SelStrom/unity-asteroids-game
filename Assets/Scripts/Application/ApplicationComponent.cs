using System;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ApplicationComponent : MonoBehaviour, IApplicationComponent
    {
        public event Action<float> OnUpdate;
        public event Action OnPause;
        public event Action OnResume;

        [SerializeField] private GameData _configs = default;
        [SerializeField] private Transform _poolContainer = default;
        [SerializeField] private Transform _gameContainer = default;
        [SerializeField] private PlayerInputProvider PlayerInputProvider = default;

        private readonly Application _application = new();

        private void Awake()
        {
            _application.Connect(this, _configs, _poolContainer, _gameContainer, PlayerInputProvider);
        }

        public void Start()
        {
            _application.Start();
        }

        private void Update()
        {
            OnUpdate?.Invoke(Time.deltaTime);
        }

        private void OnApplicationPause(bool isPause)
        {
            if (isPause)
            {
                OnPause?.Invoke();
            }
            else
            {
                OnResume?.Invoke();
            }
        }

        private void OnApplicationQuit()
        {
            _application.Quit();
        }
    }
}