using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class Model
    {
        public event Action<IGameEntity> OnEntityDead;
        
        public List<IGameEntity> Entities => _entities;
        private readonly List<IGameEntity> _entities = new();

        public Vector2 GameArea;

        public void AddEntity(IGameEntity entity)
        {
            entity.Connect(this);
            _entities.Add(entity);
        }

        public void Update(float delta)
        {
            foreach (var entity in _entities)
            {
                entity.Update(delta);
            }

            foreach (var entity in _entities.Where(x=>x.IsDead()))
            {
                OnEntityDead?.Invoke(entity);
            }
            _entities.RemoveAll(x => x.IsDead());
        }
    }
}