using System;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Application
    {
        private GameObjectPool _gameObjectPool;
        private IApplicationComponent _appComponent;
        private Game _game;
        private Model _model;
        private GameData _configs;
        private Transform _poolContainer;
        private Transform _gameContainer;
        private PlayerInputProvider _playerInputProvider;

        public void Connect(IApplicationComponent appComponent, GameData configs,
            Transform poolContainer, Transform gameContainer, PlayerInputProvider playerInputProvider)
        {
            _configs = configs;
            _playerInputProvider = playerInputProvider;
            _gameContainer = gameContainer;
            _appComponent = appComponent;

            _gameObjectPool = new GameObjectPool();
            _gameObjectPool.Connect(poolContainer);
        }

        public void Start()
        {
            var mainCamera = Camera.main;
            var orthographicSize = mainCamera.orthographicSize;
            var sceneWidth = mainCamera.aspect * orthographicSize * 2;
            var sceneHeight = orthographicSize * 2;
            Debug.Log("Scene size: " + sceneWidth + " x " + sceneHeight);

            _model = new Model { GameArea = new Vector2(sceneWidth, sceneHeight) };

            _game = new Game(_gameContainer, _model, _configs, _gameObjectPool, _playerInputProvider);
            _game.Start();

            _appComponent.OnUpdate += OnUpdate;
            _appComponent.OnPause += OnPause;
            _appComponent.OnResume += OnResume;

            _playerInputProvider.OnBackAction += OnBack;
        }

        private void OnResume()
        {
            _appComponent.OnUpdate += OnUpdate;
        }

        private void OnPause()
        {
            _appComponent.OnUpdate -= OnUpdate;
        }

        private void OnUpdate(float deltaTime)
        {
            _model.Update(deltaTime);
        }

        private void OnBack()
        {
            UnityEngine.Application.Quit(0);
        }

        private void Dispose()
        {
            _gameObjectPool.Dispose();
            _gameObjectPool = null;
            _appComponent = null;
            _game = null;
            _model = null;
            _configs = null;
            _poolContainer = null;
            _gameContainer = null;
            _playerInputProvider = null;
        }

        public void Quit()
        {
            _appComponent.OnUpdate -= OnUpdate;
            _appComponent.OnPause -= OnPause;
            _appComponent.OnResume -= OnResume;
            _playerInputProvider.OnBackAction -= OnBack;

            Dispose();
        }
    }
}