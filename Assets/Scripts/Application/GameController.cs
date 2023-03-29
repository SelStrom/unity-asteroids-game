using System;
using System.Collections.Generic;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private GameObject _bulletPrefab = default;
        [SerializeField] private Transform _poolContainer = default;
        [SerializeField] private Transform _gameContainer = default;
        [SerializeField] private InputHelper _inputHelper = default;
        [SerializeField] private ShipView ShipView = default;

        private readonly Dictionary<IGameEntity, BaseView> _modelToView = new();
        public static GameController Instance { get; private set; }
        public Model model;

        /*private void Awake()
        {
            Debug.Log("On awoke");
        }*/

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
            GameObjectPool.Connect(_poolContainer);
            
            model = new Model();

            var camera = Camera.main;
            var orthographicSize = camera.orthographicSize;
            var sceneWidth = camera.aspect * orthographicSize * 2;
            var sceneHeight = orthographicSize * 2;
            Debug.Log("Scene size: " + sceneWidth + " x " + sceneHeight);

            model.GameArea = new Vector2(sceneWidth, sceneHeight);

            var shipModel = new ShipModel();
            model.AddEntity(shipModel);

            _inputHelper.Connect(shipModel);
            ShipView.Connect(shipModel);
            ShipView.OnPositionChanged();

            model.OnEntityDead += OnEntityDead;
        }

        private void OnEntityDead(IGameEntity entity)
        {
            var view = _modelToView[entity];
            view.Dispose();
            GameObjectPool.Release(view.gameObject);

            // TODO @a.shatalov: release model to pool
        }

        public void Shoot(Vector2 position, Vector2 direction)
        {
            // TODO @a.shatalov: get model from pool
            var bullet = new BulletModel(5, position, direction);
            model.AddEntity(bullet);
            
            var bulletView = GameObjectPool.Get<BulletView>(_bulletPrefab, _gameContainer);
            bulletView.Connect(bullet);
            bulletView.OnPositionChanged();
            
            _modelToView.Add(bullet, bulletView);
        }
    }

    public class GameScreen
    {
        
    }
}