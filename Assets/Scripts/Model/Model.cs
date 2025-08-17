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
                _owner.GetSystem<GunSystem>().Add(model, model.Gun);
                _owner.GetSystem<LaserSystem>().Add(model, model.Laser);
                _owner.GetSystem<ThrustSystem>().Add(model, (model.Thrust, model.Move, model.Rotate));
            }

            void IGroupVisitor.Visit(UfoBigModel model)
            {
                _owner.GetSystem<MoveSystem>().Add(model, model.Move);
                _owner.GetSystem<GunSystem>().Add(model, model.Gun);
                _owner.GetSystem<ShootToSystem>().Add(model, (model.Move, model.Gun, model.ShootTo));
            }
            
            void IGroupVisitor.Visit(UfoModel model)
            {
                _owner.GetSystem<MoveSystem>().Add(model, model.Move);
                _owner.GetSystem<GunSystem>().Add(model, model.Gun);
                _owner.GetSystem<ShootToSystem>().Add(model, (model.Move, model.Gun, model.ShootTo));
                _owner.GetSystem<MoveToSystem>().Add(model, (model.Move, model.MoveTo));
            }
        }

        public event Action<IGameEntityModel> OnEntityDestroyed;

        private readonly Dictionary<Type, IModelSystem> _typeToSystem = new();
        private readonly LinkedList<IModelSystem> _systems = new();

        private readonly HashSet<IGameEntityModel> _entities = new();
        private readonly HashSet<IGameEntityModel> _newEntities = new();

        public Vector2 GameArea;
        private readonly IGroupVisitor _groupCreator;
        
        public int Score { get; private set; }

        public ActionScheduler ActionScheduler { get; }

        public Model()
        {
            ActionScheduler = new ActionScheduler();
            _groupCreator = new GroupCreator(this);

            RegisterSystem<RotateSystem>();
            RegisterSystem<ThrustSystem>();
            RegisterSystem<MoveSystem>().Connect(this);
            RegisterSystem<LifeTimeSystem>();
            RegisterSystem<GunSystem>();
            RegisterSystem<LaserSystem>();
            RegisterSystem<ShootToSystem>();
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
            _systems.AddLast(system);
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
        
        public void ReceiveScore(IGameEntityModel scoreHolder)
        {
            switch (scoreHolder)
            {
                case AsteroidModel ctx:
                    Score += ctx.Data.Score;
                    break;
                case UfoModel ctx:
                    Score += ctx.Data.Score;
                    break;
                case UfoBigModel ctx:
                    Score += ctx.Data.Score;
                    break;
            }
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

            foreach (var system in _systems)
            {
                system.Update(deltaTime);
            }

            foreach (var entity in _entities.Where(x => x.IsDead()))
            {
                foreach (var system in _systems)
                {
                    system.Remove(entity);
                }
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

        public void CleanUp()
        {
            foreach (var system in _systems)
            {
                system.CleanUp();
            }
            
            foreach (var entity in _entities)
            {
                OnEntityDestroyed?.Invoke(entity);
            }
            _entities.Clear();
            Score = 0;
        }
    }
}