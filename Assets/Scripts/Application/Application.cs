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
        private ActionScheduler _actionScheduler;
        private Vector2 _gameArea;
        private GameScreen _gameScreen;
        private GameData _configs;
        private Transform _gameContainer;
        private PlayerInput _playerInput;
        private EntitiesCatalog _catalog;
        private TitleScreen _titleScreen;
        private CollisionBridge _collisionBridge;
        private EntityManager _entityManager;

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

            var world = World.DefaultGameObjectInjectionWorld;
            _entityManager = world.EntityManager;

            InitializeEcsSingletons();

            _collisionBridge = new CollisionBridge();
            var collisionBufferEntity = _entityManager.CreateEntity();
            _entityManager.AddBuffer<CollisionEventData>(collisionBufferEntity);
            _collisionBridge.Initialize(_entityManager, collisionBufferEntity);

            _catalog.Connect(_configs, new ViewFactory(_gameObjectPool, _gameContainer),
                _entityManager, _collisionBridge);

            var deadSystem = world.GetExistingSystemManaged<DeadEntityCleanupSystem>();
            if (deadSystem != null)
            {
                deadSystem.SetOnDeadEntityCallback(OnDeadEntity);
            }

            var bridgeSystem = world.GetExistingSystemManaged<ObservableBridgeSystem>();
            if (bridgeSystem != null)
            {
                bridgeSystem.SetShipViewModel(
                    _catalog.GetShipViewModel(),
                    _configs.Ship.MainSprite,
                    _configs.Ship.ThrustSprite);
            }

            var shootProcessor = world.GetExistingSystemManaged<ShootEventProcessorSystem>();
            if (shootProcessor != null)
            {
                shootProcessor.SetDependencies(_catalog, _configs, _actionScheduler, _gameArea);
            }

            _game = new Game(_catalog, _actionScheduler, _gameArea, _configs, _playerInput,
                _gameScreen, _entityManager);

            _appComponent.OnUpdate += OnUpdate;
            _appComponent.OnPause += OnPause;
            _appComponent.OnResume += OnResume;

            _playerInput.OnBackAction += OnBack;

            _titleScreen.Connect(() => {
                _game.Start();

                // Обновить ShipViewModel после создания корабля
                if (bridgeSystem != null)
                {
                    bridgeSystem.SetShipViewModel(
                        _catalog.GetShipViewModel(),
                        _configs.Ship.MainSprite,
                        _configs.Ship.ThrustSprite);
                }
            });
        }

        private void InitializeEcsSingletons()
        {
            // GameAreaData singleton
            var gameAreaQuery = _entityManager.CreateEntityQuery(typeof(GameAreaData));
            if (gameAreaQuery.CalculateEntityCount() == 0)
            {
                var gameAreaEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(gameAreaEntity, new GameAreaData
                {
                    Size = new float2(_gameArea.x, _gameArea.y)
                });
            }
            else
            {
                var existingEntity = gameAreaQuery.GetSingletonEntity();
                _entityManager.SetComponentData(existingEntity, new GameAreaData
                {
                    Size = new float2(_gameArea.x, _gameArea.y)
                });
            }

            // ScoreData singleton
            var scoreQuery = _entityManager.CreateEntityQuery(typeof(ScoreData));
            if (scoreQuery.CalculateEntityCount() == 0)
            {
                var scoreEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(scoreEntity, new ScoreData { Value = 0 });
            }
            else
            {
                var existingEntity = scoreQuery.GetSingletonEntity();
                _entityManager.SetComponentData(existingEntity, new ScoreData { Value = 0 });
            }

            // GunShootEvent buffer singleton
            var gunQuery = _entityManager.CreateEntityQuery(typeof(GunShootEvent));
            if (gunQuery.CalculateEntityCount() == 0)
            {
                var gunEventEntity = _entityManager.CreateEntity();
                _entityManager.AddBuffer<GunShootEvent>(gunEventEntity);
            }
            else
            {
                var existingEntity = gunQuery.GetSingletonEntity();
                _entityManager.GetBuffer<GunShootEvent>(existingEntity).Clear();
            }

            // LaserShootEvent buffer singleton
            var laserQuery = _entityManager.CreateEntityQuery(typeof(LaserShootEvent));
            if (laserQuery.CalculateEntityCount() == 0)
            {
                var laserEventEntity = _entityManager.CreateEntity();
                _entityManager.AddBuffer<LaserShootEvent>(laserEventEntity);
            }
            else
            {
                var existingEntity = laserQuery.GetSingletonEntity();
                _entityManager.GetBuffer<LaserShootEvent>(existingEntity).Clear();
            }

            // RocketLaunchEvent buffer singleton
            var rocketQuery = _entityManager.CreateEntityQuery(typeof(RocketLaunchEvent));
            if (rocketQuery.CalculateEntityCount() == 0)
            {
                var rocketEventEntity = _entityManager.CreateEntity();
                _entityManager.AddBuffer<RocketLaunchEvent>(rocketEventEntity);
            }
            else
            {
                var existingEntity = rocketQuery.GetSingletonEntity();
                _entityManager.GetBuffer<RocketLaunchEvent>(existingEntity).Clear();
            }

            // ShipPositionData singleton
            var shipPosQuery = _entityManager.CreateEntityQuery(typeof(ShipPositionData));
            if (shipPosQuery.CalculateEntityCount() == 0)
            {
                var shipPosEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(shipPosEntity, new ShipPositionData
                {
                    Position = float2.zero,
                    Speed = 0f,
                    Direction = float2.zero
                });
            }
            else
            {
                var existingEntity = shipPosQuery.GetSingletonEntity();
                _entityManager.SetComponentData(existingEntity, new ShipPositionData
                {
                    Position = float2.zero,
                    Speed = 0f,
                    Direction = float2.zero
                });
            }
        }

        private void OnDeadEntity(DeadEntityInfo info)
        {
            var go = info.GameObject;
            _collisionBridge.UnregisterMapping(go);
            var position = (Vector2)go.transform.position;

            if (_catalog.TryGetEntityType(go, out var entityType))
            {
                if (entityType == EntityType.Asteroid)
                {
                    _game.PlayEffect(_configs.VfxBlowPrefab, position);
                    var age = info.Age - 1;
                    if (age > 0)
                    {
                        var speed = Math.Min(info.Speed * 2, 10f);
                        _catalog.CreateAsteroid(age, position, speed);
                        _catalog.CreateAsteroid(age, position, speed);
                    }
                }
                else if (entityType == EntityType.Ship)
                {
                    _game.PlayEffect(_configs.VfxBlowPrefab, position);
                    _game.StopGame();
                }
                else if (entityType == EntityType.UfoBig || entityType == EntityType.Ufo)
                {
                    _game.PlayEffect(_configs.VfxBlowPrefab, position);
                }
                else if (entityType == EntityType.Rocket)
                {
                    _game.PlayEffect(_configs.VfxBlowPrefab, position);
                }
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
            _actionScheduler = null;
            _configs = null;
            _gameContainer = null;
            _playerInput = null;
            _gameScreen = null;
            _collisionBridge = null;
        }

        public void Quit()
        {
            _appComponent.OnUpdate -= OnUpdate;
            _appComponent.OnPause -= OnPause;
            _appComponent.OnResume -= OnResume;
            _playerInput.OnBackAction -= OnBack;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var shootProcessor = world.GetExistingSystemManaged<ShootEventProcessorSystem>();
                if (shootProcessor != null)
                {
                    shootProcessor.ClearDependencies();
                }

                var bridgeSystem = world.GetExistingSystemManaged<ObservableBridgeSystem>();
                if (bridgeSystem != null)
                {
                    bridgeSystem.ClearReferences();
                }

                var deadSystem = world.GetExistingSystemManaged<DeadEntityCleanupSystem>();
                if (deadSystem != null)
                {
                    deadSystem.SetOnDeadEntityCallback(null);
                }
            }

            Dispose();
        }
    }
}
