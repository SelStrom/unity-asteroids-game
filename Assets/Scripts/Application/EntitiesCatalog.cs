using System;
using System.Collections.Generic;
using System.IO;
using SelStrom.Asteroids.Configs;
using SelStrom.Asteroids.ECS;
using Shtl.Mvvm;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public enum EntityType
    {
        Ship,
        Asteroid,
        Bullet,
        UfoBig,
        Ufo,
        Rocket
    }

    public class EntitiesCatalog
    {
        private readonly Dictionary<GameObject, Entity> _gameObjectToEntity = new();
        private readonly Dictionary<GameObject, EntityType> _gameObjectToEntityType = new();
        private readonly Dictionary<GameObject, EventBindingContext> _gameObjectToBindings = new();

        private ViewFactory _viewFactory;
        private GameData _configs;

        private EntityManager _entityManager;
        private CollisionBridge _collisionBridge;

        private ShipViewModel _lastShipViewModel;

        public ViewFactory ViewFactory => _viewFactory;

        public void Connect(GameData configs, ViewFactory viewFactory, EntityManager entityManager,
            CollisionBridge collisionBridge)
        {
            _viewFactory = viewFactory;
            _configs = configs;
            _entityManager = entityManager;
            _collisionBridge = collisionBridge;
        }

        public ShipViewModel GetShipViewModel()
        {
            return _lastShipViewModel;
        }

        public bool TryGetEntityType(GameObject go, out EntityType entityType)
        {
            return _gameObjectToEntityType.TryGetValue(go, out entityType);
        }

        public bool TryGetEntity(GameObject go, out Entity entity)
        {
            return _gameObjectToEntity.TryGetValue(go, out entity);
        }

        public void ReleaseByGameObject(GameObject go)
        {
            if (_gameObjectToBindings.TryGetValue(go, out var bindings))
            {
                bindings.CleanUp();
                _gameObjectToBindings.Remove(go);
            }

            if (_gameObjectToEntity.TryGetValue(go, out var entity))
            {
                if (_entityManager.Exists(entity))
                {
                    _entityManager.DestroyEntity(entity);
                }

                _gameObjectToEntity.Remove(go);
            }

            _gameObjectToEntityType.Remove(go);

            _collisionBridge.UnregisterMapping(go);
            _viewFactory.Release(go);
        }

        public void CreateShip()
        {
            var viewModel = new ShipViewModel();
            _lastShipViewModel = viewModel;
            var bindings = new EventBindingContext();

            viewModel.Sprite.Value = _configs.Ship.MainSprite;
            bindings.InvokeAll();

            var view = _viewFactory.Get<ShipVisual>(_configs.Ship.Prefab);
            view.Connect(viewModel);

            var entity = EntityFactory.CreateShip(
                _entityManager,
                float2.zero,
                0f,
                _configs.Ship.ThrustUnitsPerSecond,
                _configs.Ship.MaxSpeed,
                _configs.Ship.Gun.MaxShoots,
                _configs.Ship.Gun.ReloadDurationSec,
                _configs.Laser.LaserMaxShoots,
                _configs.Laser.LaserUpdateDurationSec,
                rocketMaxAmmo: _configs.Rocket.MaxAmmo,
                rocketReloadSec: _configs.Rocket.ReloadDurationSec
            );
            _entityManager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = view.transform,
                GameObject = view.gameObject
            });
            _collisionBridge.RegisterMapping(view.gameObject, entity);

            viewModel.OnCollision.Value = (col) =>
            {
                _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
            };

            AddToCatalog(view.gameObject, entity, EntityType.Ship, bindings);
        }

        public void CreateBullet(GameData.BulletData data, GameObject prefab, Vector2 position, Vector2 direction)
        {
            var viewModel = new BulletViewModel();
            var bindings = new EventBindingContext();
            bindings.InvokeAll();

            var view = _viewFactory.Get<BulletVisual>(prefab);
            view.Connect(viewModel);

            var isPlayer = (prefab == _configs.Bullet.Prefab);
            var entity = EntityFactory.CreateBullet(
                _entityManager,
                new float2(position.x, position.y),
                data.Speed,
                new float2(direction.x, direction.y),
                data.LifeTimeSeconds,
                isPlayer
            );
            _entityManager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = view.transform,
                GameObject = view.gameObject
            });
            _collisionBridge.RegisterMapping(view.gameObject, entity);

            viewModel.OnCollision.Value = col =>
            {
                _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
            };

            AddToCatalog(view.gameObject, entity, EntityType.Bullet, bindings);
        }

        public void CreateAsteroid(int size, Vector2 position, float speed)
        {
            var data = size switch
            {
                3 => _configs.AsteroidBig,
                2 => _configs.AsteroidMedium,
                1 => _configs.AsteroidSmall,
                var _ => throw new InvalidDataException()
            };

            var dir = Random.insideUnitCircle.normalized;

            var viewModel = new AsteroidViewModel();
            var bindings = new EventBindingContext();

            viewModel.Sprite.Value = data.SpriteVariants[Random.Range(0, data.SpriteVariants.Length)];
            bindings.InvokeAll();

            var view = _viewFactory.Get<AsteroidVisual>(data.Prefab);
            view.Connect(viewModel);

            var entity = EntityFactory.CreateAsteroid(
                _entityManager,
                new float2(position.x, position.y),
                speed,
                new float2(dir.x, dir.y),
                size,
                data.Score
            );
            _entityManager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = view.transform,
                GameObject = view.gameObject
            });
            _collisionBridge.RegisterMapping(view.gameObject, entity);

            viewModel.OnCollision.Value = (col) =>
            {
                _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
            };

            AddToCatalog(view.gameObject, entity, EntityType.Asteroid, bindings);
        }

        public void CreateBigUfo(Vector2 position, Vector2 direction)
        {
            var viewModel = new UfoViewModel();
            var bindings = new EventBindingContext();
            bindings.InvokeAll();

            var view = _viewFactory.Get<UfoVisual>(_configs.UfoBig.Prefab);
            view.Connect(viewModel);

            var entity = EntityFactory.CreateUfoBig(
                _entityManager,
                new float2(position.x, position.y),
                _configs.UfoBig.Speed,
                new float2(direction.x, direction.y),
                _configs.UfoBig.Gun.MaxShoots,
                _configs.UfoBig.Gun.ReloadDurationSec,
                _configs.UfoBig.Score
            );
            _entityManager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = view.transform,
                GameObject = view.gameObject
            });
            _collisionBridge.RegisterMapping(view.gameObject, entity);

            viewModel.OnCollision.Value = (col) =>
            {
                _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
            };

            AddToCatalog(view.gameObject, entity, EntityType.UfoBig, bindings);
        }

        public void CreateUfo(Vector2 position, Vector2 direction)
        {
            var viewModel = new UfoViewModel();
            var bindings = new EventBindingContext();
            bindings.InvokeAll();

            var view = _viewFactory.Get<UfoVisual>(_configs.Ufo.Prefab);
            view.Connect(viewModel);

            var entity = EntityFactory.CreateUfo(
                _entityManager,
                new float2(position.x, position.y),
                _configs.Ufo.Speed,
                new float2(direction.x, direction.y),
                _configs.Ufo.Gun.MaxShoots,
                _configs.Ufo.Gun.ReloadDurationSec,
                3f,
                _configs.Ufo.Score
            );
            _entityManager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = view.transform,
                GameObject = view.gameObject
            });
            _collisionBridge.RegisterMapping(view.gameObject, entity);

            viewModel.OnCollision.Value = (col) =>
            {
                _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
            };

            AddToCatalog(view.gameObject, entity, EntityType.Ufo, bindings);
        }

        public void CreateRocket(Vector2 position, Vector2 direction)
        {
            var viewModel = new RocketViewModel();
            var bindings = new EventBindingContext();
            bindings.InvokeAll();

            var view = _viewFactory.Get<RocketVisual>(_configs.Rocket.Prefab);
            view.Connect(viewModel);

            var entity = EntityFactory.CreateRocket(
                _entityManager,
                new float2(position.x, position.y),
                _configs.Rocket.Speed,
                new float2(direction.x, direction.y),
                _configs.Rocket.LifeTimeSec,
                _configs.Rocket.TurnRateDegPerSec,
                _configs.Rocket.Score
            );
            _entityManager.AddComponentObject(entity, new GameObjectRef
            {
                Transform = view.transform,
                GameObject = view.gameObject
            });
            _collisionBridge.RegisterMapping(view.gameObject, entity);

            viewModel.OnCollision.Value = col =>
            {
                _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
            };

            AddToCatalog(view.gameObject, entity, EntityType.Rocket, bindings);
        }

        private void AddToCatalog(GameObject go, Entity entity, EntityType type, EventBindingContext bindings)
        {
            _gameObjectToEntity[go] = entity;
            _gameObjectToEntityType[go] = type;
            _gameObjectToBindings[go] = bindings;
        }

        public void ReleaseAllGameEntities()
        {
            var gameObjects = new List<GameObject>(_gameObjectToEntity.Keys);
            foreach (var go in gameObjects)
            {
                ReleaseByGameObject(go);
            }
        }

        public void CleanUp()
        {
            foreach (var bindings in _gameObjectToBindings.Values)
            {
                bindings.CleanUp();
            }

            _gameObjectToBindings.Clear();

            foreach (var entity in _gameObjectToEntity.Values)
            {
                if (_entityManager.Exists(entity))
                {
                    _entityManager.DestroyEntity(entity);
                }
            }

            _collisionBridge?.Clear();
            _gameObjectToEntity.Clear();
            _gameObjectToEntityType.Clear();
        }

        public void Dispose()
        {
            CleanUp();
            _viewFactory = null;
            _configs = null;
        }
    }
}
