using System;
using System.Collections.Generic;
using System.IO;
using SelStrom.Asteroids.Configs;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class GameScreen
    {
        private readonly GameObjectPool _gameObjectPool;
        private readonly InputHelper _inputHelper;
        private readonly Transform _gameContainer;
        private readonly GameData _configs;
        private readonly Model _model;

        private readonly Dictionary<IGameEntityModel, BaseVisual> _modelToView = new();
        private readonly Dictionary<GameObject, AsteroidModel> _gameObjectToAsteroidModel = new();

        private ShipModel _shipModel;

        public GameScreen(Transform gameContainer, Model model, GameData configs, GameObjectPool gameObjectPool,
            InputHelper inputHelper)
        {
            _gameContainer = gameContainer;
            _model = model;
            _configs = configs;
            _gameObjectPool = gameObjectPool;
            _inputHelper = inputHelper;
        }

        public void Start()
        {
            _model.OnEntityDestroyed += OnEntityDestroyed;

            _shipModel = CreateShip(OnShipCollided);
            for (var i = 0; i < _configs.AsteroidInitialCount; i++)
            {
                SpawnAsteroid(_shipModel.Move.Position.Value);
            }

            _inputHelper.OnAttackAction += OnAttack;
            _inputHelper.OnRotateAction += OnRotateAction;
            _inputHelper.OnTrustAction += OnTrust;
            //TODO _inputHelper.OnLaserAction += ;
        }

        private void SpawnAsteroid(Vector2 shipPosition)
        {
            var gameArea = _model.GameArea;
            var asteroidPosition = new Vector2(Random.Range(0, gameArea.x), Random.Range(0, gameArea.y)) - gameArea / 2;

            var distance = shipPosition - asteroidPosition;
            var allowedDistance = distance.magnitude - _configs.AsteroidSpawnAllowedRadius;
            if (allowedDistance < 0)
            {
                asteroidPosition += distance.normalized * allowedDistance;
            }

            CreateAsteroid(3, asteroidPosition, Random.Range(1f, 3f));
        }

        private ShipModel CreateShip(Action<Collision2D> onRegisterCollision)
        {
            var model = CreateModel<ShipModel>();

            var view = CreateView<ShipVisual>(_configs.ShipPrefab);
            view.Connect(new ShipVisualData
            {
                ShipModel = model,
                OnRegisterCollision = onRegisterCollision
            });
            _modelToView.Add(model, view);
            return model;
        }

        private void CreateBullet(Vector2 position, Vector2 direction,
            Action<BulletModel, Collision2D> onRegisterCollision)
        {
            var model = CreateModel<BulletModel>();
            model.SetData(_configs.Bullet, position, direction, _configs.Bullet.Speed);

            var view = CreateView<BulletVisual>(_configs.Bullet.Prefab);
            view.Connect(new BulletVisualData
            {
                BulletModel = model,
                OnRegisterCollision = onRegisterCollision,
            });
            _modelToView.Add(model, view);
        }

        private void CreateAsteroid(int age, Vector2 position, float speed)
        {
            if (age <= 0)
            {
                return;
            }

            var data = age switch
            {
                3 => _configs.AsteroidBig,
                2 => _configs.AsteroidMedium,
                1 => _configs.AsteroidSmall,
                var _ => throw new InvalidDataException()
            };

            var model = CreateModel<AsteroidModel>();
            model.SetData(data, age, position, Random.insideUnitCircle, speed);

            var view = CreateView<AsteroidVisual>(data.Prefab);
            _modelToView.Add(model, view);
            view.Connect((model.Move.Position, model.Data.SpriteVariants));
            
            _gameObjectToAsteroidModel.Add(view.gameObject, model);
        }

        private void OnShipCollided(Collision2D obj)
        {
            Kill(_shipModel);
            // TODO @a.shatalov: complete game;
        }

        private void OnBulletCollided(BulletModel bullet, Collision2D col)
        {
            Kill(bullet);
            
            var asteroidModel = _gameObjectToAsteroidModel[col.gameObject];
            _gameObjectToAsteroidModel.Remove(col.gameObject);
            Kill(asteroidModel);

            var age = asteroidModel.Age - 1;
            var position = asteroidModel.Move.Position.Value;
            var speed = Math.Min(asteroidModel.Move.Speed * 2, 10f);
            CreateAsteroid(age, position, speed);
            CreateAsteroid(age, position, speed);
        }

        private TModel CreateModel<TModel>() where TModel : class, IGameEntityModel, new()
        {
            // TODO @a.shatalov: pool
            var model = new TModel();
            _model.AddEntity(model);
            return model;
        }

        private TView CreateView<TView>(GameObject prefab) where TView : BaseVisual
        {
            return _gameObjectPool.Get<TView>(prefab, _gameContainer);
        }

        private void OnEntityDestroyed(IGameEntityModel entityModel)
        {
            ReleaseEntity(entityModel);
        }

        private void ReleaseEntity(IGameEntityModel entityModel)
        {
            var view = _modelToView[entityModel];
            view.Dispose();
            _modelToView.Remove(entityModel);
            _gameObjectPool.Release(view.gameObject);
            // TODO @a.shatalov: release model to pool
        }

        private static void Kill(IGameEntityModel entityModel)
        {
            entityModel.Kill();
        }

        #region InputHandlers

        private void OnAttack()
        {
            var forwardOffset = _shipModel.Rotate.Rotation.Value;
            CreateBullet(_shipModel.Move.Position.Value + forwardOffset, _shipModel.Rotate.Rotation.Value, OnBulletCollided);
        }

        private void OnRotateAction(InputValue inputValue)
        {
            _shipModel.Rotate.TargetDirection = inputValue.Get<float>();
        }

        private void OnTrust(InputValue inputValue)
        {
            _shipModel.Thrust.IsActive.Value = inputValue.isPressed;
        }

        #endregion

    }

    public class EntityFactory
    {
    }
}