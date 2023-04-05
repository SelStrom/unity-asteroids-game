using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Model
    {
        public event Action<IGameEntityModel> OnEntityDestroyed;

        public HashSet<IGameEntityModel> Entities => _entities;
        private readonly HashSet<IGameEntityModel> _entities = new();
        
        private readonly HashSet<IGameEntityModel> _newEntities = new();

        public Vector2 GameArea;

        public void AddEntity(IGameEntityModel entityModel)
        {
            entityModel.Connect(this);
            _newEntities.Add(entityModel);
        }

        public void Update(float delta)
        {
            if (_newEntities.Any())
            {
                _entities.UnionWith(_newEntities);
                _newEntities.Clear();
            }
            
            foreach (var entity in _entities)
            {
                entity.Update(delta);
            }

            foreach (var entity in _entities.Where(x=>x.IsDead()))
            {
                OnEntityDestroyed?.Invoke(entity);
            }
            
            _entities.RemoveWhere(x => x.IsDead());
        }

    }
}