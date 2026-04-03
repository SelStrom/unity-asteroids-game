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
        private ActionScheduler _actionScheduler;
        private Vector2 _gameArea;
        private GameScreen _gameScreen;
        private GameData _configs;
        private Transform _gameContainer;
        private PlayerInput _playerInput;
        private EntitiesCatalog _catalog;
        private TitleScreen _titleScreen;

        public void Connect(IApplicationComponent appComponent, GameData configs,
            Transform poolContainer, Transform gameContainer, GameScreen gameScreen, TitleScreen titleScreen)
        {
            _titleScreen = titleScreen;
            _gameScreen = gameScreen;
            _configs = configs;
            _playerInput = new PlayerInput();
            _gameContainer = gameContainer;
            _appComponent = appComponent;

            _gameObjectPool = new GameObjectPool();
            _gameObjectPool.Connect(poolContainer);
            
            _catalog = new EntitiesCatalog();
        }

        public void Start()
        {
            var mainCamera = Camera.main;
            var orthographicSize = mainCamera!.orthographicSize;
            var sceneWidth = mainCamera.aspect * orthographicSize * 2;
            var sceneHeight = orthographicSize * 2;
            Debug.Log("Scene size: " + sceneWidth + " x " + sceneHeight);

            _gameArea = new Vector2(sceneWidth, sceneHeight);
            _actionScheduler = new ActionScheduler();
            _model = new Model { GameArea = _gameArea };

            _catalog.Connect(_configs, new ModelFactory(_model), new ViewFactory(_gameObjectPool, _gameContainer));

            _game = new Game(_catalog, _model, _actionScheduler, _gameArea, _configs, _playerInput, _gameScreen);

            _appComponent.OnUpdate += OnUpdate;
            _appComponent.OnPause += OnPause;
            _appComponent.OnResume += OnResume;

            _playerInput.OnBackAction += OnBack;

            _titleScreen.Connect(() => {
                _game.Start();
            });
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
            _actionScheduler.Update(deltaTime);
        }

        private void OnBack()
        {
#if !UNITY_WEBGL
            UnityEngine.Application.Quit(0);
#endif
        }

        private void Dispose()
        {
            _catalog.Dispose();
            _gameObjectPool.Dispose();
            
            _catalog = null;
            _gameObjectPool = null;
            _appComponent = null;
            _game = null;
            _model = null;
            _actionScheduler = null;
            _configs = null;
            _gameContainer = null;
            _playerInput = null;
            _gameScreen = null;
        }

        public void Quit()
        {
            _appComponent.OnUpdate -= OnUpdate;
            _appComponent.OnPause -= OnPause;
            _appComponent.OnResume -= OnResume;
            _playerInput.OnBackAction -= OnBack;

            Dispose();
        }
    }
}