using System;
using System.Collections.Generic;
using System.IO;
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

        public void Connect(GameData configs, ModelFactory modelFactory, ViewFactory viewFactory)
        {
            _modelFactory = modelFactory;
            _viewFactory = viewFactory;
            _configs = configs;
        }

        public TModel FindModel<TModel>(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<AsteroidVisual>(out var asteroidVisual))
            {
                throw new Exception($"Unable to find {typeof(TModel)} in gameObject {gameObject.name}");
            }

            return (TModel)_visualToModel[asteroidVisual];
        }

        public ShipModel CreateShip(Action<Collision2D> onRegisterCollision)
        {
            var model = _modelFactory.Get<ShipModel>();
            model.SetData(_configs.Ship);

            var view = _viewFactory.Get<ShipVisual>(_configs.Ship.Prefab);
            view.Connect(new ShipVisualData
            {
                ShipModel = model,
                OnRegisterCollision = onRegisterCollision
            });
            AddToCatalog(model, view);
            return model;
        }

        public void CreateBullet(Vector2 position, Vector2 direction,
            Action<BulletModel, Collision2D> onRegisterCollision)
        {
            var model = _modelFactory.Get<BulletModel>();
            model.SetData(_configs.Bullet, position, direction, _configs.Bullet.Speed);

            var view = _viewFactory.Get<BulletVisual>(_configs.Bullet.Prefab);
            view.Connect(new BulletVisualData
            {
                BulletModel = model,
                OnRegisterCollision = onRegisterCollision,
            });
            AddToCatalog(model, view);
        }

        public void CreateAsteroid(int age, Vector2 position, float speed)
        {
            var data = age switch
            {
                3 => _configs.AsteroidBig,
                2 => _configs.AsteroidMedium,
                1 => _configs.AsteroidSmall,
                var _ => throw new InvalidDataException()
            };

            var model = _modelFactory.Get<AsteroidModel>();
            model.SetData(data, age, position, Random.insideUnitCircle, speed);

            var view = _viewFactory.Get<AsteroidVisual>(data.Prefab);
            AddToCatalog(model, view);
            view.Connect((model.Move.Position, model.Data.SpriteVariants));
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