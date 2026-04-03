using System;
using System.Collections.Generic;
using System.IO;
using Model.Components;
using SelStrom.Asteroids.Configs;
using SelStrom.Asteroids.ECS;
using Shtl.Mvvm;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class EntitiesCatalog
    {
        private readonly Dictionary<IGameEntityModel, IEntityView> _modelToVisual = new();
        private readonly Dictionary<IEntityView, IGameEntityModel> _visualToModel = new();
        private readonly Dictionary<GameObject, IGameEntityModel> _gameObjectToModel = new();
        private readonly Dictionary<IGameEntityModel, EventBindingContext> _modelToBindings = new();
        private readonly Dictionary<GameObject, Entity> _gameObjectToEntity = new();

        private ModelFactory _modelFactory;
        private ViewFactory _viewFactory;
        private GameData _configs;

        private EntityManager _entityManager;
        private CollisionBridge _collisionBridge;
        private bool _useEcs;

        private ShipViewModel _lastShipViewModel;

        public ViewFactory ViewFactory => _viewFactory;

        public void Connect(GameData configs, ModelFactory modelFactory, ViewFactory viewFactory)
        {
            _modelFactory = modelFactory;
            _viewFactory = viewFactory;
            _configs = configs;
        }

        public void ConnectEcs(EntityManager entityManager, CollisionBridge collisionBridge)
        {
            _entityManager = entityManager;
            _collisionBridge = collisionBridge;
            _useEcs = true;
        }

        public ShipViewModel GetShipViewModel()
        {
            return _lastShipViewModel;
        }

        public bool TryFindModel<TModel>(GameObject gameObject, out TModel model)
            where TModel : IGameEntityModel
        {
            if (_gameObjectToModel.TryGetValue(gameObject, out var modelBase) && modelBase is TModel typed)
            {
                model = typed;
                return true;
            }

            model = default;
            return false;
        }

        public bool TryGetEntity(GameObject go, out Entity entity)
        {
            return _gameObjectToEntity.TryGetValue(go, out entity);
        }

        public void ReleaseByGameObject(GameObject go)
        {
            if (_gameObjectToModel.TryGetValue(go, out var model))
            {
                if (_modelToBindings.TryGetValue(model, out var bindings))
                {
                    bindings.CleanUp();
                    _modelToBindings.Remove(model);
                }

                var view = _modelToVisual[model];
                _modelToVisual.Remove(model);
                _visualToModel.Remove(view);
                _gameObjectToModel.Remove(go);

                _viewFactory.Release(view);
                _modelFactory.Release(model);
            }

            if (_gameObjectToEntity.TryGetValue(go, out _))
            {
                _gameObjectToEntity.Remove(go);
            }
        }

        public ShipModel CreateShip(Action<Collision2D> onRegisterCollision)
        {
            var model = _modelFactory.Get<ShipModel>();
            model.SetData(_configs.Ship);
            model.Thrust.MaxSpeed = _configs.Ship.MaxSpeed;
            model.Thrust.UnitsPerSecond = _configs.Ship.ThrustUnitsPerSecond;
            model.Gun.MaxShoots = _configs.Ship.Gun.MaxShoots;
            model.Gun.ReloadDurationSec = _configs.Ship.Gun.ReloadDurationSec;
            model.Laser.MaxShoots = _configs.Laser.LaserMaxShoots;
            model.Laser.CurrentShoots.Value = _configs.Laser.LaserMaxShoots;
            model.Laser.UpdateDurationSec = _configs.Laser.LaserUpdateDurationSec;
            model.Laser.ReloadRemaining.Value = _configs.Laser.LaserUpdateDurationSec;

            var viewModel = new ShipViewModel();
            _lastShipViewModel = viewModel;
            var bindings = new EventBindingContext();

            bindings.From(model.Move.Position).To(viewModel.Position);
            bindings.From(model.Rotate.Rotation).To(viewModel.Rotation);
            bindings.From(model.Thrust.IsActive).To(viewModel.Sprite,
                (bool isThrust, ReactiveValue<Sprite> sprite) =>
                    sprite.Value = isThrust ? _configs.Ship.ThrustSprite : _configs.Ship.MainSprite);

            viewModel.OnCollision.Value = onRegisterCollision;

            bindings.InvokeAll();

            var view = _viewFactory.Get<ShipVisual>(_configs.Ship.Prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);

            if (_useEcs)
            {
                var entity = EntityFactory.CreateShip(
                    _entityManager,
                    new float2(model.Move.Position.Value.x, model.Move.Position.Value.y),
                    0f, // Ship starts stationary
                    _configs.Ship.ThrustUnitsPerSecond,
                    _configs.Ship.MaxSpeed,
                    _configs.Ship.Gun.MaxShoots,
                    _configs.Ship.Gun.ReloadDurationSec,
                    _configs.Laser.LaserMaxShoots,
                    _configs.Laser.LaserUpdateDurationSec
                );
                _entityManager.AddComponentObject(entity, new GameObjectRef
                {
                    Transform = view.transform,
                    GameObject = view.gameObject
                });
                _collisionBridge.RegisterMapping(view.gameObject, entity);
                _gameObjectToEntity[view.gameObject] = entity;

                viewModel.OnCollision.Value = (col) =>
                {
                    _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
                };
            }

            return model;
        }

        public void CreateBullet(GameData.BulletData data, GameObject prefab, Vector2 position, Vector2 direction,
            Action<BulletModel, Collision2D> onRegisterCollision)
        {
            var model = _modelFactory.Get<BulletModel>();
            model.SetData(data, position, direction, data.Speed);

            var viewModel = new BulletViewModel();
            var bindings = new EventBindingContext();

            bindings.From(model.Move.Position).To(viewModel.Position);

            if (_useEcs)
            {
                // Collision will be set after view creation
            }
            else
            {
                viewModel.OnCollision.Value = col => onRegisterCollision(model, col);
            }

            bindings.InvokeAll();

            var view = _viewFactory.Get<BulletVisual>(prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);

            if (_useEcs)
            {
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
                _gameObjectToEntity[view.gameObject] = entity;

                viewModel.OnCollision.Value = col =>
                {
                    _collisionBridge.ReportCollision(view.gameObject, col.gameObject);
                };
            }
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

            var model = _modelFactory.Get<AsteroidModel>();
            model.SetData(data, size, position, Random.insideUnitCircle, speed);

            var viewModel = new AsteroidViewModel();
            var bindings = new EventBindingContext();

            bindings.From(model.Move.Position).To(viewModel.Position);
            viewModel.Sprite.Value = data.SpriteVariants[Random.Range(0, data.SpriteVariants.Length)];
            bindings.InvokeAll();

            var view = _viewFactory.Get<AsteroidVisual>(data.Prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);

            if (_useEcs)
            {
                var entity = EntityFactory.CreateAsteroid(
                    _entityManager,
                    new float2(model.Move.Position.Value.x, model.Move.Position.Value.y),
                    model.Move.Speed.Value,
                    new float2(model.Move.Direction.x, model.Move.Direction.y),
                    size,
                    data.Score
                );
                _entityManager.AddComponentObject(entity, new GameObjectRef
                {
                    Transform = view.transform,
                    GameObject = view.gameObject
                });
                _collisionBridge.RegisterMapping(view.gameObject, entity);
                _gameObjectToEntity[view.gameObject] = entity;
            }
        }

        public void CreateBigUfo(ShipModel ship, Vector2 position, Vector2 direction,
            Action<UfoBigModel> onRegisterCollision, Action<GunComponent> onGunShooting)
        {
            var model = _modelFactory.Get<UfoBigModel>();
            model.SetData(_configs.UfoBig, position, direction, _configs.UfoBig.Speed);
            model.ShootTo.Ship = ship;
            model.Gun.MaxShoots = _configs.UfoBig.Gun.MaxShoots;
            model.Gun.ReloadDurationSec = _configs.UfoBig.Gun.ReloadDurationSec;
            model.Gun.ReloadRemaining = _configs.UfoBig.Gun.ReloadDurationSec;
            model.Gun.OnShooting = onGunShooting;

            var viewModel = new UfoViewModel();
            var bindings = new EventBindingContext();

            bindings.From(model.Move.Position).To(viewModel.Position);

            if (_useEcs)
            {
                // Collision callback set after view creation
            }
            else
            {
                viewModel.OnCollision.Value = () => onRegisterCollision(model);
            }

            bindings.InvokeAll();

            var view = _viewFactory.Get<UfoVisual>(_configs.UfoBig.Prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);

            if (_useEcs)
            {
                var entity = EntityFactory.CreateUfoBig(
                    _entityManager,
                    new float2(position.x, position.y),
                    _configs.UfoBig.Speed,
                    new float2(direction.x, direction.y),
                    _configs.UfoBig.Gun.MaxShoots,
                    _configs.UfoBig.Gun.ReloadDurationSec,
                    3f, // shootToEvery -- hardcoded 1:1 with original
                    _configs.UfoBig.Score
                );
                _entityManager.AddComponentObject(entity, new GameObjectRef
                {
                    Transform = view.transform,
                    GameObject = view.gameObject
                });
                _collisionBridge.RegisterMapping(view.gameObject, entity);
                _gameObjectToEntity[view.gameObject] = entity;

                // UfoVisual collision doesn't pass col.gameObject, so we can't use CollisionBridge directly
                // UfoVisual.OnCollisionEnter2D calls Action without params
                // We leave the original callback for UFO collisions in non-ECS path
            }
        }

        public void CreateUfo(ShipModel ship, Vector2 position, Vector2 direction,
            Action<UfoBigModel> onRegisterCollision, Action<GunComponent> onGunShooting)
        {
            var model = _modelFactory.Get<UfoModel>();
            model.SetData(_configs.UfoBig, position, direction, _configs.Ufo.Speed);
            model.ShootTo.Ship = ship;
            model.MoveTo.Ship = ship;
            model.MoveTo.Every = 3f;
            model.Gun.MaxShoots = _configs.Ufo.Gun.MaxShoots;
            model.Gun.ReloadDurationSec = _configs.Ufo.Gun.ReloadDurationSec;
            model.Gun.ReloadRemaining = _configs.Ufo.Gun.ReloadDurationSec;
            model.Gun.OnShooting = onGunShooting;

            var viewModel = new UfoViewModel();
            var bindings = new EventBindingContext();

            bindings.From(model.Move.Position).To(viewModel.Position);

            if (_useEcs)
            {
                // Collision callback set after view creation
            }
            else
            {
                viewModel.OnCollision.Value = () => onRegisterCollision(model);
            }

            bindings.InvokeAll();

            var view = _viewFactory.Get<UfoVisual>(_configs.Ufo.Prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);

            if (_useEcs)
            {
                var entity = EntityFactory.CreateUfo(
                    _entityManager,
                    new float2(position.x, position.y),
                    _configs.Ufo.Speed,
                    new float2(direction.x, direction.y),
                    _configs.Ufo.Gun.MaxShoots,
                    _configs.Ufo.Gun.ReloadDurationSec,
                    3f, // shootToEvery -- hardcoded 1:1 with original
                    3f, // moveToEvery -- hardcoded 1:1 with original
                    _configs.Ufo.Score
                );
                _entityManager.AddComponentObject(entity, new GameObjectRef
                {
                    Transform = view.transform,
                    GameObject = view.gameObject
                });
                _collisionBridge.RegisterMapping(view.gameObject, entity);
                _gameObjectToEntity[view.gameObject] = entity;
            }
        }

        private void AddToCatalog(IGameEntityModel model, IEntityView view, EventBindingContext bindings)
        {
            _modelToVisual.Add(model, view);
            _visualToModel.Add(view, model);
            _gameObjectToModel.Add(view.gameObject, model);
            _modelToBindings.Add(model, bindings);
        }

        public void Release(IGameEntityModel model)
        {
            // Модель могла быть уже удалена через ReleaseByGameObject (например, DeadEntityCleanupSystem)
            if (!_modelToVisual.TryGetValue(model, out var view))
            {
                return;
            }

            if (_modelToBindings.TryGetValue(model, out var bindings))
            {
                bindings.CleanUp();
                _modelToBindings.Remove(model);
            }

            _modelToVisual.Remove(model);
            _visualToModel.Remove(view);
            _gameObjectToModel.Remove(view.gameObject);

            if (_useEcs)
            {
                if (_gameObjectToEntity.TryGetValue(view.gameObject, out var entity))
                {
                    _gameObjectToEntity.Remove(view.gameObject);
                    if (_entityManager.Exists(entity))
                    {
                        _entityManager.DestroyEntity(entity);
                    }
                }
            }

            _viewFactory.Release(view);
            _modelFactory.Release(model);
        }

        public void CleanUp()
        {
            foreach (var bindings in _modelToBindings.Values)
            {
                bindings.CleanUp();
            }

            _modelToBindings.Clear();
            _modelToVisual.Clear();
            _visualToModel.Clear();
            _gameObjectToModel.Clear();

            if (_useEcs)
            {
                // Уничтожаем оставшиеся ECS-entity (если не были уничтожены в Release)
                foreach (var entity in _gameObjectToEntity.Values)
                {
                    if (_entityManager.Exists(entity))
                    {
                        _entityManager.DestroyEntity(entity);
                    }
                }

                _collisionBridge?.Clear();
                _gameObjectToEntity.Clear();
            }
        }

        public void Dispose()
        {
            CleanUp();
            _modelFactory = null;
            _viewFactory = null;
            _configs = null;
        }
    }
}
