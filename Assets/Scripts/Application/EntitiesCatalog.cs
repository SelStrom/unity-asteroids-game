using System;
using System.Collections.Generic;
using System.IO;
using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class EntitiesCatalog
    {
        private readonly Dictionary<IGameEntityModel, BaseVisual> _modelToVisual = new();
        private readonly Dictionary<BaseVisual, IGameEntityModel> _visualToModel = new();

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

        public bool TryFindModel<TModel, TVisual>(GameObject gameObject, out TModel model) 
            where TModel : IGameEntityModel
            where TVisual : BaseVisual
        {
            if (!gameObject.TryGetComponent<TVisual>(out var visual)
                || !_visualToModel.TryGetValue(visual, out var modelBase))
            {
                model = default;   
                return false;
            }

            model = (TModel)modelBase;
            return true;
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
            
            var view = _viewFactory.Get<ShipVisual>(_configs.Ship.Prefab);
            view.Connect(new ShipVisualData
            {
                ShipModel = model,
                OnRegisterCollision = onRegisterCollision
            });
            AddToCatalog(model, view);
            return model;
        }

        public void CreateBullet(GameData.BulletData data, GameObject prefab, Vector2 position, Vector2 direction,
            Action<BulletModel, Collision2D> onRegisterCollision)
        {
            var model = _modelFactory.Get<BulletModel>();
            model.SetData(data, position, direction, data.Speed);

            var view = _viewFactory.Get<BulletVisual>(prefab);
            view.Connect(new BulletVisualData
            {
                BulletModel = model,
                OnRegisterCollision = onRegisterCollision,
            });
            AddToCatalog(model, view);
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

            var view = _viewFactory.Get<AsteroidVisual>(data.Prefab);
            view.Connect((model.Move.Position, model.Data.SpriteVariants));
            AddToCatalog(model, view);
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
            
            var view = _viewFactory.Get<UfoVisual>(_configs.UfoBig.Prefab);
            view.Connect(new UfoVisualData()
            {
                UfoModel = model,
                OnRegisterCollision = onRegisterCollision,
            });

            AddToCatalog(model, view);
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
            
            var view = _viewFactory.Get<UfoVisual>(_configs.Ufo.Prefab);
            view.Connect(new UfoVisualData()
            {
                UfoModel = model,
                OnRegisterCollision = onRegisterCollision,
            });

            AddToCatalog(model, view);
        }

        private void AddToCatalog(IGameEntityModel model, BaseVisual view)
        {
            _modelToVisual.Add(model, view);
            _visualToModel.Add(view, model);
        }

        public void Release(IGameEntityModel model)
        {
            var view = _modelToVisual[model];

            _modelToVisual.Remove(model);
            _visualToModel.Remove(view);

            _viewFactory.Release(view);
            _modelFactory.Release(model);
        }

        public void CleanUp()
        {
            _modelToVisual.Clear();
            _visualToModel.Clear();
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