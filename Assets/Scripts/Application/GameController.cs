using System;
using System.Collections.Generic;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private GameObject _shipPrefab = default;
        [SerializeField] private GameObject _bulletPrefab = default;

        [SerializeField] private GameObject _asteroidBigPrefab = default;
        [SerializeField] private GameObject _asteroidMediumPrefab = default;
        [SerializeField] private GameObject _asteroidSmallPrefab = default;

        [SerializeField] private GameObject _ufoMediumPrefab = default;
        [SerializeField] private GameObject _ufoSmallPrefab = default;


        [SerializeField] private Transform _poolContainer = default;
        [SerializeField] private Transform _gameContainer = default;
        [SerializeField] private InputHelper _inputHelper = default;
        [SerializeField] private ShipView ShipView = default;

        private readonly Dictionary<IGameEntity, BaseView> _modelToView = new();
        public static GameController Instance { get; private set; }
        public Model model;

        private readonly GameObjectPool _gameObjectPool = new();
        
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
            model.Update(deltaTime);
        }

        private void Initialize()
        {
            _gameObjectPool.Connect(_poolContainer);

            model = new Model();

            var camera = Camera.main;
            var orthographicSize = camera.orthographicSize;
            var sceneWidth = camera.aspect * orthographicSize * 2;
            var sceneHeight = orthographicSize * 2;
            Debug.Log("Scene size: " + sceneWidth + " x " + sceneHeight);

            model.GameArea = new Vector2(sceneWidth, sceneHeight);
            CreateShip();

            _inputHelper.Connect(this);

            model.OnEntityDead += OnEntityDead;
        }

        private void OnEntityDead(IGameEntity entity)
        {
            var view = _modelToView[entity];
            view.Dispose();
            _gameObjectPool.Release(view.gameObject);

            // TODO @a.shatalov: release model to pool
        }

        private void CreateShip()
        {
            _shipModel = new ShipModel();
            model.AddEntity(_shipModel);

            ShipView = _gameObjectPool.Get<ShipView>(_shipPrefab, _gameContainer);
            ShipView.Connect(_shipModel);
        }

        public void CreateBullet(Vector2 position, Vector2 direction)
        {
            // TODO @a.shatalov: get model from pool
            var entity = new BulletModel(5, position, direction, 20f);
            model.AddEntity(entity);

            var bulletView = _gameObjectPool.Get<BulletView>(_bulletPrefab, _gameContainer);
            bulletView.Connect(entity);

            _modelToView.Add(entity, bulletView);
        }

        public void CreateAsteroid(Vector2 position, Vector2 direction, int age)
        {
            // TODO @a.shatalov: get model from pool
            var entity = new AsteroidModel(age, position, direction, 20f);
            model.AddEntity(entity);

            var view = _gameObjectPool.Get<AsteroidView>(_bulletPrefab, _gameContainer);
            view.Connect(entity);

            _modelToView.Add(entity, view);
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
    }

    public class GameScreen
    {
    }
}