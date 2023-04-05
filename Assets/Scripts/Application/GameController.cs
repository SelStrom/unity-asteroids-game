using System;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        [SerializeField] private GameData _configs = default;
        [SerializeField] private Transform _poolContainer = default;
        [SerializeField] private Transform _gameContainer = default;
        [SerializeField] private InputHelper _inputHelper = default;

        private readonly GameObjectPool _gameObjectPool = new();

        private DateTime _lastUpdateTime = DateTime.Now;
        private GameScreen _gameScreen;
        private Model _model;

        private void OnEnable()
        {
            Instance = this;
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
            _gameObjectPool.Connect(_poolContainer);

            var camera1 = Camera.main;
            var orthographicSize = camera1.orthographicSize;
            var sceneWidth = camera1.aspect * orthographicSize * 2;
            var sceneHeight = orthographicSize * 2;
            Debug.Log("Scene size: " + sceneWidth + " x " + sceneHeight);

            _model = new Model { GameArea = new Vector2(sceneWidth, sceneHeight) };
            _gameScreen = new GameScreen(_gameContainer, _model, _configs, _gameObjectPool, _inputHelper);
            _gameScreen.Start();
        }

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
            _model.Update(deltaTime);
        }
    }
}