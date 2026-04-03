using System;
using SelStrom.Asteroids.Configs;
using SelStrom.Asteroids.ECS;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SelStrom.Asteroids
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(DeadEntityCleanupSystem))]
    public partial class ShootEventProcessorSystem : SystemBase
    {
        private EntitiesCatalog _catalog;
        private GameData _configs;
        private ActionScheduler _actionScheduler;
        private Vector2 _gameArea;

        public void SetDependencies(EntitiesCatalog catalog, GameData configs,
            ActionScheduler actionScheduler, Vector2 gameArea)
        {
            _catalog = catalog;
            _configs = configs;
            _actionScheduler = actionScheduler;
            _gameArea = gameArea;
        }

        public void ClearDependencies()
        {
            _catalog = null;
            _configs = null;
            _actionScheduler = null;
        }

        protected override void OnUpdate()
        {
            if (_catalog == null || _configs == null)
            {
                return;
            }

            ProcessGunEvents();
            ProcessLaserEvents();
        }

        private void ProcessGunEvents()
        {
            foreach (var buffer in SystemAPI.Query<DynamicBuffer<GunShootEvent>>())
            {
                if (buffer.Length == 0)
                {
                    continue;
                }

                var events = buffer.ToNativeArray(Allocator.Temp);
                buffer.Clear();

                for (int i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    var position = new Vector2(evt.Position.x, evt.Position.y);
                    var direction = new Vector2(evt.Direction.x, evt.Direction.y);
                    var prefab = evt.IsPlayer ? _configs.Bullet.Prefab : _configs.Bullet.EnemyPrefab;
                    _catalog.CreateBullet(_configs.Bullet, prefab, position, direction);
                }

                events.Dispose();
            }
        }

        private void ProcessLaserEvents()
        {
            foreach (var buffer in SystemAPI.Query<DynamicBuffer<LaserShootEvent>>())
            {
                if (buffer.Length == 0)
                {
                    continue;
                }

                var events = buffer.ToNativeArray(Allocator.Temp);
                buffer.Clear();

                for (int i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    var position = new Vector2(evt.Position.x, evt.Position.y);
                    var direction = new Vector2(evt.Direction.x, evt.Direction.y);

                    var effect = _catalog.ViewFactory.Get<LineRenderer>(_configs.Laser.Prefab);
                    effect.transform.position = position;
                    var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    effect.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
                    _actionScheduler.ScheduleAction(
                        () => { _catalog.ViewFactory.Release(effect.gameObject); },
                        _configs.Laser.BeamEffectLifetimeSec);

                    var hits = new RaycastHit2D[30];
                    var size = Physics2D.RaycastNonAlloc(position, direction, hits,
                        _gameArea.magnitude, LayerMask.GetMask("Asteroid", "Enemy"));
                    if (size <= 0)
                    {
                        continue;
                    }

                    for (var j = 0; j < size; j++)
                    {
                        var hit = hits[j];
                        var gameObject = hit.collider.gameObject;
                        if (_catalog.TryGetEntity(gameObject, out var hitEntity))
                        {
                            if (EntityManager.Exists(hitEntity) && !EntityManager.HasComponent<DeadTag>(hitEntity))
                            {
                                // Начисляем очки
                                if (SystemAPI.HasSingleton<ScoreData>())
                                {
                                    var scoreEntity = SystemAPI.GetSingletonEntity<ScoreData>();
                                    var scoreData = EntityManager.GetComponentData<ScoreData>(scoreEntity);
                                    if (EntityManager.HasComponent<ScoreValue>(hitEntity))
                                    {
                                        scoreData.Value +=
                                            EntityManager.GetComponentData<ScoreValue>(hitEntity).Score;
                                        EntityManager.SetComponentData(scoreEntity, scoreData);
                                    }
                                }

                                EntityManager.AddComponent<DeadTag>(hitEntity);
                            }
                        }
                    }
                }

                events.Dispose();
            }
        }
    }
}
