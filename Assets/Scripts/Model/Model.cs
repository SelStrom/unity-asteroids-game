using System;
using System.Collections.Generic;
using System.Linq;
using Model.Components;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Model : IGroupHolder
    {
        public event Action<IGameEntityModel> OnEntityDestroyed;

        private readonly HashSet<IGameEntityModel> _entities = new();
        private readonly HashSet<MoveComponent> _movable = new();
        private readonly HashSet<RotateComponent> _rotatable = new();
        private readonly HashSet<LifeTimeComponent> _lifeLimited = new();
        private readonly HashSet<(ThrustComponent, MoveComponent, RotateComponent)> _thrustable = new();
        
        private readonly HashSet<IGameEntityModel> _newEntities = new();

        public Vector2 GameArea;

        public void AddEntity(IGameEntityModel entityModel)
        {
            _newEntities.Add(entityModel);
        }
        
        void IGroupHolder.Group(AsteroidModel model)
        {
            _movable.Add(model.Move);
        }

        void IGroupHolder.Group(BulletModel model)
        {
            _movable.Add(model.Move);
            _lifeLimited.Add(model.LifeTime);
        }

        void IGroupHolder.Group(ShipModel model)
        {
            _movable.Add(model.Move);
            _rotatable.Add(model.Rotate);
            _thrustable.Add((model.Thrust, model.Move, model.Rotate));
        }

        public void Update(float deltaTime)
        {
            if (_newEntities.Any())
            {
                _entities.UnionWith(_newEntities);
                foreach (var entity in _newEntities)
                {
                    entity.ConnectWith(this);
                }
                
                _newEntities.Clear();
            }
            
            foreach (var com in _rotatable)
            {
                UpdateRotation(com, deltaTime);
            }

            foreach (var com in _thrustable)
            {
                UpdateThrust(com, deltaTime);
            }

            foreach (var com in _movable)
            {
                UpdateMove(com, GameArea, deltaTime);
            }

            foreach (var com in _lifeLimited)
            {
                UpdateLifeRemaining(com, deltaTime);
            }

            foreach (var entity in _entities.Where(x=>x.IsDead()))
            {
                OnEntityDestroyed?.Invoke(entity);
            }
            
            _entities.RemoveWhere(x => x.IsDead());
        }

        private static void UpdateThrust((ThrustComponent Thrust, MoveComponent Move, RotateComponent Rotate) com, float deltaTime)
        {
            if (com.Thrust.IsActive.Value)
            {
                var acceleration = ThrustComponent.UnitsPerSecond * deltaTime;
                var velocity = com.Move.Direction * com.Move.Speed + com.Rotate.Rotation.Value * acceleration;
                
                com.Move.Direction = velocity.normalized;
                com.Move.Speed = Math.Min(velocity.magnitude, ThrustComponent.MaxSpeed);
            }
            else
            {
                com.Move.Speed = Math.Max(com.Move.Speed - ThrustComponent.UnitsPerSecond / 2 * deltaTime, ThrustComponent.MinSpeed);
            }
        }

        private static void UpdateLifeRemaining(LifeTimeComponent com, float deltaTime)
        {
            com.TimeRemaining = Math.Max(com.TimeRemaining - deltaTime, 0);
        }

        private static void UpdateRotation(RotateComponent com, float deltaTime)
        {
            if (com.TargetDirection == 0)
            {
                return;
            }

            com.Rotation.Value =
                Quaternion.Euler(0, 0, RotateComponent.DegreePerSecond * deltaTime * com.TargetDirection) *
                com.Rotation.Value;
        }

        private static void UpdateMove(MoveComponent com, Vector2 gameArea, float deltaTime)
        {
            var oldPosition = com.Position.Value;
            var position = oldPosition + com.Direction * (com.Speed * deltaTime);
            PlaceWithinGameArea(ref position.x, gameArea.x);
            PlaceWithinGameArea(ref position.y, gameArea.y);
            com.Position.Value = position;
        }

        private static void PlaceWithinGameArea(ref float position, float side)
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