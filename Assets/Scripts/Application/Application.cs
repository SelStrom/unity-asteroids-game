using System;
using SelStrom.Asteroids.Configs;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Application
    {
        private GameObjectPool _gameObjectPool;
        private IApplicationComponent _appComponent;
        private Game _game;
        private Model _model;
        private GameScreen _gameScreen;
        private GameData _configs;
        private Transform _gameContainer;
        private PlayerInput _playerInput;
        private EntitiesCatalog _catalog;
        private TitleScreen _titleScreen;

        private bool _useEcs = true;
        private CollisionBridge _collisionBridge;

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

            _model = new Model { GameArea = new Vector2(sceneWidth, sceneHeight) };

            _catalog.Connect(_configs, new ModelFactory(_model), new ViewFactory(_gameObjectPool, _gameContainer));

            if (_useEcs)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                var em = world.EntityManager;

                // Singleton entities
                var gameAreaEntity = em.CreateEntity();
                em.AddComponentData(gameAreaEntity, new GameAreaData
                {
                    Size = new float2(sceneWidth, sceneHeight)
                });

                var shipPosEntity = em.CreateEntity();
                em.AddComponentData(shipPosEntity, new ShipPositionData());

                var scoreEntity = em.CreateEntity();
                em.AddComponentData(scoreEntity, new ScoreData { Value = 0 });

                var collisionBufferEntity = em.CreateEntity();
                em.AddBuffer<CollisionEventData>(collisionBufferEntity);

                var gunEventEntity = em.CreateEntity();
                em.AddBuffer<GunShootEvent>(gunEventEntity);

                var laserEventEntity = em.CreateEntity();
                em.AddBuffer<LaserShootEvent>(laserEventEntity);

                // CollisionBridge
                _collisionBridge = new CollisionBridge();
                _collisionBridge.Initialize(em, collisionBufferEntity);

                // DeadEntityCleanupSystem callback
                var cleanupSystem = world.GetExistingSystemManaged<DeadEntityCleanupSystem>();
                if (cleanupSystem != null)
                {
                    cleanupSystem.SetOnDeadEntityCallback(OnDeadEntity);
                }

                // EntitiesCatalog ECS
                _catalog.ConnectEcs(em, _collisionBridge);
            }

            _game = new Game(_catalog, _model, _configs, _playerInput, _gameScreen);

            if (_useEcs)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                var em = world.EntityManager;
                _game.ConnectEcs(_useEcs, _collisionBridge, em);
            }

            _appComponent.OnUpdate += OnUpdate;
            _appComponent.OnPause += OnPause;
            _appComponent.OnResume += OnResume;

            _playerInput.OnBackAction += OnBack;

            _titleScreen.Connect(() => {
                _game.Start();

                if (_useEcs)
                {
                    var world = World.DefaultGameObjectInjectionWorld;
                    var bridge = world.GetExistingSystemManaged<ObservableBridgeSystem>();
                    if (bridge != null)
                    {
                        bridge.SetShipViewModel(
                            _catalog.GetShipViewModel(),
                            _configs.Ship.MainSprite,
                            _configs.Ship.ThrustSprite);
                    }
                }
            });
        }

        private void OnDeadEntity(GameObject go)
        {
            _collisionBridge.UnregisterMapping(go);

            if (_catalog.TryFindModel<AsteroidModel>(go, out var asteroidModel))
            {
                _game.PlayEffect(_configs.VfxBlowPrefab, asteroidModel.Move.Position.Value);
                var age = asteroidModel.Age - 1;
                if (age > 0)
                {
                    var position = asteroidModel.Move.Position.Value;
                    var speed = Math.Min(asteroidModel.Move.Speed.Value * 2, 10f);
                    _catalog.CreateAsteroid(age, position, speed);
                    _catalog.CreateAsteroid(age, position, speed);
                }
            }
            else if (_catalog.TryFindModel<ShipModel>(go, out var shipModel))
            {
                _game.PlayEffect(_configs.VfxBlowPrefab, shipModel.Move.Position.Value);
                _game.StopFromEcs();
            }
            else if (_catalog.TryFindModel<UfoBigModel>(go, out var ufoModel))
            {
                _game.PlayEffect(_configs.VfxBlowPrefab, ufoModel.Move.Position.Value);
            }

            _catalog.ReleaseByGameObject(go);
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
            if (_useEcs)
            {
                // ECS World updates automatically via Unity runtime
                _model.ActionScheduler.Update(deltaTime);
                _game.ProcessShootEvents();
            }
            else
            {
                _model.Update(deltaTime);
            }
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

            if (_collisionBridge != null)
            {
                _collisionBridge.Clear();
                _collisionBridge = null;
            }

            Dispose();
        }
    }
}
