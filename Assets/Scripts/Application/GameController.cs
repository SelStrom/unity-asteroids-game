using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SelStrom.Asteroids.Configs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private GameData _configs = default;
        [SerializeField] private Transform _poolContainer = default;
        [SerializeField] private Transform _gameContainer = default;
        [SerializeField] private InputHelper _inputHelper = default;

        private readonly Dictionary<IGameEntityModel, BaseView> _modelToView = new();
        private readonly Dictionary<GameObject, AsteroidModel> _gameObjectToAsteroidModel = new();
        public static GameController Instance { get; private set; }

        public Model Model;

        private readonly GameObjectPool _gameObjectPool = new();

        #region Unity part

        private void OnEnable()
        {
            Instance = this;

            Debug.Log("On enabled");
        }

        private void OnDisable()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
        }

        private void Start()
        {
            Initialize();
        }

        private DateTime _lastUpdateTime = DateTime.Now;
        private ShipModel _shipModel;

        private void Update()
        {
            if (Instance != this)
            {
                return;
            }

            var currentUpdateTime = DateTime.Now;
            var deltaTimeSpan = currentUpdateTime - _lastUpdateTime;
            _lastUpdateTime = currentUpdateTime;
            var deltaTime = (float)deltaTimeSpan.TotalSeconds * Time.timeScale;
            Model.Update(deltaTime);
        }

        #endregion

        private void Initialize()
        {
            _gameObjectPool.Connect(_poolContainer);

            var camera = Camera.main;
            var orthographicSize = camera.orthographicSize;
            var sceneWidth = camera.aspect * orthographicSize * 2;
            var sceneHeight = orthographicSize * 2;
            Debug.Log("Scene size: " + sceneWidth + " x " + sceneHeight);

            Model = new Model { GameArea = new Vector2(sceneWidth, sceneHeight) };

            _inputHelper.Connect(this);

            Model.OnEntityDestroyed += OnEntityDestroyed;

            StarGame();
        }

        private void StarGame()
        {
            _shipModel = CreateShip();
            const int asteroidInitialCount = 10;
            for (var i = 0; i < asteroidInitialCount; i++)
            {
                SpawnAsteroid(_shipModel.Move.Position.Value);
            }
        }

        private ShipModel CreateShip()
        {
            var entity = CreateModel<ShipModel>();
            var view = CreateView<ShipView>(entity, _configs.ShipPrefab);
            view.Connect((entity, this));
            return entity;
        }

        private void CreateBullet(Vector2 position, Vector2 direction)
        {
            var entity = CreateModel<BulletModel>();
            entity.SetData(_configs.Bullet, position, direction);

            var view = CreateView<BulletView>(entity, _configs.BulletPrefab);
            view.Connect((entity, this));
        }
        
        private void CreateAsteroid(int age, Vector2 position, float speed)
        {
            if (age <= 0)
            {
                return;
            }

            var entity = CreateModel<AsteroidModel>();
            entity.SetData(age, position, Random.insideUnitCircle, speed);

            var prefab = age switch
            {
                3 => _configs.AsteroidBigPrefab,
                2 => _configs.AsteroidMediumPrefab,
                1 => _configs.AsteroidSmallPrefab,
                var _ => throw new InvalidDataException()
            };
            var view = CreateView<AsteroidView>(entity, prefab);
            _gameObjectToAsteroidModel.Add(view.gameObject, entity);
            view.Connect(entity);
        }

        private TModel CreateModel<TModel>() where TModel : class, IGameEntityModel, new()
        {
            // TODO @a.shatalov: pool
            var entityModel = new TModel();
            Model.AddEntity(entityModel);
            return entityModel;
        }

        private TView CreateView<TView>(IGameEntityModel model, GameObject prefab) where TView : BaseView
        {
            var view = _gameObjectPool.Get<TView>(prefab, _gameContainer);    
            _modelToView.Add(model, view);
            return view;
        }
        
        private void OnEntityDestroyed(IGameEntityModel entityModel)
        {
            var view = _modelToView[entityModel];
            view.Dispose();
            _modelToView.Remove(entityModel);
            _gameObjectPool.Release(view.gameObject);
            // TODO @a.shatalov: release model to pool
        }

        private void SpawnAsteroid(Vector2 shipPosition)
        {
            var gameArea = Model.GameArea;
            var asteroidPosition = new Vector2(Random.Range(0, gameArea.x), Random.Range(0, gameArea.y)) - gameArea / 2;

            var distance = shipPosition - asteroidPosition;
            var allowedDistance = distance.magnitude - _configs.AsteroidSpawnAllowedRadius;
            if (allowedDistance < 0)
            {
                asteroidPosition += distance.normalized * allowedDistance;
            }
            
            CreateAsteroid(3, asteroidPosition, Random.Range(1f, 3f));
        }

        public void ShipRotate(float rotationDirection)
        {
            _shipModel.RotationDirection = rotationDirection;
        }

        public void ShipThrust(bool thrust)
        {
            _shipModel.Thrust.Value = thrust;
        }

        public void ShipShoot()
        {
            var forwardOffset = _shipModel.Rotation.Value;
            CreateBullet(_shipModel.Move.Position.Value + forwardOffset, _shipModel.Rotation.Value);
        }

        public void Kill(IGameEntityModel entityModel)
        {
            entityModel.Kill();
        }

        public void KillBullet(BulletModel bulletModel)
        {
            Kill(bulletModel);
        }

        public void KillAsteroid(GameObject asteroid)
        {
            var asteroidModel = _gameObjectToAsteroidModel[asteroid];
            Kill(asteroidModel);
            _gameObjectToAsteroidModel.Remove(asteroid);
            
            var age = asteroidModel.Age - 1;
            var position = asteroidModel.Move.Position.Value;
            var speed = Math.Min(asteroidModel.Move.Speed * 2, 10f);
            CreateAsteroid(age, position, speed);
            CreateAsteroid(age, position, speed);
        }
    }

    public class EntityFactory
    {
    }
}