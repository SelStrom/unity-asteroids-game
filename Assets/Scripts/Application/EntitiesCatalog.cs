using System;
using System.Collections.Generic;
using System.IO;
using Model.Components;
using SelStrom.Asteroids.Configs;
using Shtl.Mvvm;
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

        private ModelFactory _modelFactory;
        private ViewFactory _viewFactory;
        private GameData _configs;

        public ViewFactory ViewFactory => _viewFactory;

        public void Connect(GameData configs, ModelFactory modelFactory, ViewFactory viewFactory)
        {
            _modelFactory = modelFactory;
            _viewFactory = viewFactory;
            _configs = configs;
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
            viewModel.OnCollision.Value = col => onRegisterCollision(model, col);
            bindings.InvokeAll();

            var view = _viewFactory.Get<BulletVisual>(prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);
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
            viewModel.OnCollision.Value = () => onRegisterCollision(model);
            bindings.InvokeAll();

            var view = _viewFactory.Get<UfoVisual>(_configs.UfoBig.Prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);
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
            viewModel.OnCollision.Value = () => onRegisterCollision(model);
            bindings.InvokeAll();

            var view = _viewFactory.Get<UfoVisual>(_configs.Ufo.Prefab);
            view.Connect(viewModel);
            AddToCatalog(model, view, bindings);
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
            var view = _modelToVisual[model];

            if (_modelToBindings.TryGetValue(model, out var bindings))
            {
                bindings.CleanUp();
                _modelToBindings.Remove(model);
            }

            _modelToVisual.Remove(model);
            _visualToModel.Remove(view);
            _gameObjectToModel.Remove(view.gameObject);

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
