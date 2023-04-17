using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Model
    {
        private class GroupCreator : IGroupVisitor
        {
            private readonly Model _owner;

            public GroupCreator(Model model)
            {
                _owner = model;
            }

            void IGroupVisitor.Visit(AsteroidModel model)
            {
                _owner.GetSystem<MoveSystem>().Add(model, model.Move);
            }

            void IGroupVisitor.Visit(BulletModel model)
            {
                _owner.GetSystem<MoveSystem>().Add(model, model.Move);
                _owner.GetSystem<LifeTimeSystem>().Add(model, model.LifeTime);
            }

            void IGroupVisitor.Visit(ShipModel model)
            {
                _owner.GetSystem<MoveSystem>().Add(model, model.Move);
                _owner.GetSystem<RotateSystem>().Add(model, model.Rotate);
                _owner.GetSystem<ThrustSystem>().Add(model, (model.Thrust, model.Move, model.Rotate));
            }

            void IGroupVisitor.Visit(UfoBigModel model)
            {
                _owner.GetSystem<MoveSystem>().Add(model, model.Move);
                _owner.GetSystem<ShootToSystem>().Add(model, (model.Move, model.ShootTo));
            }
            
            void IGroupVisitor.Visit(UfoModel model)
            {
                _owner.GetSystem<MoveSystem>().Add(model, model.Move);
                _owner.GetSystem<ShootToSystem>().Add(model, (model.Move, model.ShootTo));
                _owner.GetSystem<MoveToSystem>().Add(model, (model.Move, model.MoveTo));
            }
        }

        private class GroupRemover : IGroupVisitor
        {
            private readonly Model _owner;

            public GroupRemover(Model model)
            {
                _owner = model;
            }

            void IGroupVisitor.Visit(AsteroidModel model)
            {
                _owner.GetSystem<MoveSystem>().Remove(model);
            }

            void IGroupVisitor.Visit(BulletModel model)
            {
                _owner.GetSystem<MoveSystem>().Remove(model);
                _owner.GetSystem<LifeTimeSystem>().Remove(model);
            }

            void IGroupVisitor.Visit(ShipModel model)
            {
                _owner.GetSystem<MoveSystem>().Remove(model);
                _owner.GetSystem<RotateSystem>().Remove(model);
                _owner.GetSystem<ThrustSystem>().Remove(model);
            }

            void IGroupVisitor.Visit(UfoBigModel model)
            {
                _owner.GetSystem<MoveSystem>().Remove(model);
                _owner.GetSystem<ShootToSystem>().Remove(model);
            }
            
            void IGroupVisitor.Visit(UfoModel model)
            {
                _owner.GetSystem<MoveSystem>().Remove(model);
                _owner.GetSystem<ShootToSystem>().Remove(model);
                _owner.GetSystem<MoveToSystem>().Remove(model);
            }
        }

        public Action<Vector2, Vector2> OnShootReady;
        public event Action<IGameEntityModel> OnEntityDestroyed;

        private readonly Dictionary<Type, IModelSystem> _typeToSystem = new();
        private readonly HashSet<IGameEntityModel> _entities = new();
        private readonly HashSet<IGameEntityModel> _newEntities = new();

        public Vector2 GameArea;
        private readonly IGroupVisitor _groupCreator;
        private readonly IGroupVisitor _groupRemover;

        public ActionScheduler ActionScheduler { get; }

        public Model()
        {
            ActionScheduler = new ActionScheduler();
            _groupCreator = new GroupCreator(this);
            _groupRemover = new GroupRemover(this);

            RegisterSystem<RotateSystem>();
            RegisterSystem<ThrustSystem>();
            RegisterSystem<MoveSystem>().Connect(this);
            RegisterSystem<LifeTimeSystem>();
            RegisterSystem<ShootToSystem>().Connect(this);
            RegisterSystem<MoveToSystem>();
        }

        public void AddEntity(IGameEntityModel entityModel)
        {
            _newEntities.Add(entityModel);
        }

        private TSystem RegisterSystem<TSystem>() where TSystem : class, IModelSystem
        {
            var system = (TSystem)Activator.CreateInstance(typeof(TSystem));
            _typeToSystem.Add(typeof(TSystem), system);
            return system;
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, IModelSystem
        {
            if (!_typeToSystem.TryGetValue(typeof(TSystem), out var system))
            {
                system = RegisterSystem<TSystem>();
            }

            return (TSystem)system;
        }

        public void Update(float deltaTime)
        {
            ActionScheduler.Update(deltaTime);

            if (_newEntities.Any())
            {
                _entities.UnionWith(_newEntities);
                foreach (var entity in _newEntities)
                {
                    entity.AcceptWith(_groupCreator);
                }

                _newEntities.Clear();
            }

            foreach (var system in _typeToSystem.Values)
            {
                system.Update(deltaTime);
            }

            foreach (var entity in _entities.Where(x => x.IsDead()))
            {
                entity.AcceptWith(_groupRemover);
                OnEntityDestroyed?.Invoke(entity);
            }

            _entities.RemoveWhere(x => x.IsDead());
        }

        public static void PlaceWithinGameArea(ref float position, float side)
        {
            if (position > side / 2)
            {
                position = -side + position;
            }

            if (position < -side / 2)
            {
                position = side - position;
            }
        }
    }
}