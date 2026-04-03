using System;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ApplicationEntry : MonoBehaviour, IApplicationComponent
    {
        public event Action<float> OnUpdate;
        public event Action OnPause;
        public event Action OnResume;

        [SerializeField] private GameData _configs = default;
        [SerializeField] private Transform _poolContainer = default;
        [SerializeField] private Transform _gameContainer = default;
        [SerializeField] private HudVisual _hudVisual = default;
        [SerializeField] private ScoreVisual _scoreVisual = default;
        [SerializeField] private TitleScreenView _titleScreenView = default;

        private readonly Application _application = new();

        private void Awake()
        {
            var authProxy = new UnityAuthProxy();
            var leaderboardProxy = new UnityLeaderboardProxy();
            var leaderboardService = new LeaderboardService(authProxy, leaderboardProxy, _configs.LeaderboardId, this);

            _application.Connect(this, _configs, _poolContainer, _gameContainer,
                new GameScreen(_hudVisual, _scoreVisual, _configs, leaderboardService, this),
                new TitleScreen(_titleScreenView));
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

        private void OnDestroy()
        {
            _application.Quit();
        }
    }
}
