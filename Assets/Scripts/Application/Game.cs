using System;
using SelStrom.Asteroids.Configs;
using SelStrom.Asteroids.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class Game
    {
        private readonly PlayerInput _playerInput;
        private readonly GameData _configs;
        private readonly ActionScheduler _actionScheduler;
        private readonly Vector2 _gameArea;
        private readonly EntitiesCatalog _catalog;
        private readonly GameScreen _gameScreen;
        private readonly EntityManager _entityManager;

        private int _currentScore;

        public Game(EntitiesCatalog catalog, ActionScheduler actionScheduler, Vector2 gameArea,
            GameData configs, PlayerInput playerInput, GameScreen gameScreen, EntityManager entityManager)
        {
            _gameScreen = gameScreen;
            _actionScheduler = actionScheduler;
            _gameArea = gameArea;
            _configs = configs;
            _playerInput = playerInput;
            _catalog = catalog;
            _entityManager = entityManager;
        }

        public void Start()
        {
            _catalog.CreateShip();

            for (var i = 0; i < _configs.AsteroidInitialCount; i++)
            {
                SpawnAsteroid(Vector2.zero);
            }

            _playerInput.OnAttackAction += OnAttack;
            _playerInput.OnRotateAction += OnRotateAction;
            _playerInput.OnTrustAction += OnTrust;
            _playerInput.OnLaserAction += OnLaser;
            _playerInput.OnRocketAction += OnRocket;

            _actionScheduler.ScheduleAction(SpawnNewEnemy, _configs.SpawnNewEnemyDurationSec);

            _gameScreen.Connect(new GameScreenData
            {
                Game = this,
            });
            _gameScreen.ToggleState(GameScreen.State.Game);
        }

        private void Stop()
        {
            _actionScheduler.ResetSchedule();

            _playerInput.OnAttackAction -= OnAttack;
            _playerInput.OnRotateAction -= OnRotateAction;
            _playerInput.OnTrustAction -= OnTrust;
            _playerInput.OnLaserAction -= OnLaser;
            _playerInput.OnRocketAction -= OnRocket;

            _gameScreen.ToggleState(GameScreen.State.EndGame);
        }

        public void StopGame()
        {
            ReadScoreFromEcs();
            Stop();
        }

        private void ReadScoreFromEcs()
        {
            var query = _entityManager.CreateEntityQuery(typeof(ScoreData));
            if (query.CalculateEntityCount() > 0)
            {
                var entity = query.GetSingletonEntity();
                _currentScore = _entityManager.GetComponentData<ScoreData>(entity).Value;
            }
        }

        public int GetCurrentScore()
        {
            ReadScoreFromEcs();
            return _currentScore;
        }

        public void Restart()
        {
            _catalog.ReleaseAllGameEntities();
            _actionScheduler.ResetSchedule();
            ClearEcsEventBuffers();
            ResetEcsScore();
            Start();
        }

        private void ClearEcsEventBuffers()
        {
            var gunQuery = _entityManager.CreateEntityQuery(typeof(GunShootEvent));
            if (gunQuery.CalculateEntityCount() > 0)
            {
                var entities = gunQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    _entityManager.GetBuffer<GunShootEvent>(entities[i]).Clear();
                }

                entities.Dispose();
            }

            var laserQuery = _entityManager.CreateEntityQuery(typeof(LaserShootEvent));
            if (laserQuery.CalculateEntityCount() > 0)
            {
                var entities = laserQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    _entityManager.GetBuffer<LaserShootEvent>(entities[i]).Clear();
                }

                entities.Dispose();
            }

            var rocketQuery = _entityManager.CreateEntityQuery(typeof(RocketShootEvent));
            if (rocketQuery.CalculateEntityCount() > 0)
            {
                var entities = rocketQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    _entityManager.GetBuffer<RocketShootEvent>(entities[i]).Clear();
                }

                entities.Dispose();
            }

            var collisionQuery = _entityManager.CreateEntityQuery(typeof(CollisionEventData));
            if (collisionQuery.CalculateEntityCount() > 0)
            {
                var entities = collisionQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                for (int i = 0; i < entities.Length; i++)
                {
                    _entityManager.GetBuffer<CollisionEventData>(entities[i]).Clear();
                }

                entities.Dispose();
            }
        }

        private void ResetEcsScore()
        {
            var query = _entityManager.CreateEntityQuery(typeof(ScoreData));
            if (query.CalculateEntityCount() > 0)
            {
                var entity = query.GetSingletonEntity();
                _entityManager.SetComponentData(entity, new ScoreData { Value = 0 });
            }
        }

        private Vector2 GetShipPosition()
        {
            var query = _entityManager.CreateEntityQuery(typeof(ShipPositionData));
            if (query.CalculateEntityCount() > 0)
            {
                var data = query.GetSingleton<ShipPositionData>();
                return new Vector2(data.Position.x, data.Position.y);
            }

            return Vector2.zero;
        }

        private void SpawnNewEnemy()
        {
            var shipPosition = GetShipPosition();
            var index = Random.Range(0, 3);
            switch (index)
            {
                case 0:
                    SpawnAsteroid(shipPosition);
                    break;
                case 1:
                    SpawnUfo(shipPosition);
                    break;
                case 2:
                    SpawnBigUfo(shipPosition);
                    break;
            }

            _actionScheduler.ScheduleAction(SpawnNewEnemy, _configs.SpawnNewEnemyDurationSec);
        }

        private void SpawnUfo(Vector2 shipPosition)
        {
            var position = GameUtils.GetRandomUfoPosition(shipPosition, _gameArea, _configs.SpawnAllowedRadius);
            _catalog.CreateUfo(position, Random.insideUnitCircle.normalized);
        }

        private void SpawnBigUfo(Vector2 shipPosition)
        {
            var position = GameUtils.GetRandomUfoPosition(shipPosition, _gameArea, _configs.SpawnAllowedRadius);
            _catalog.CreateBigUfo(position,
                (Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized);
        }

        private void SpawnAsteroid(Vector2 shipPosition)
        {
            var asteroidPosition =
                GameUtils.GetRandomAsteroidPosition(shipPosition, _gameArea, _configs.SpawnAllowedRadius);
            _catalog.CreateAsteroid(3, asteroidPosition, Random.Range(1f, 3f));
        }

        public void PlayEffect(GameObject prefab, in Vector2 position)
        {
            var effect = _catalog.ViewFactory.Get<EffectVisual>(prefab);
            effect.transform.position = position;
            effect.Connect(OnEffectStopped);
        }

        private void OnEffectStopped(EffectVisual effect)
        {
            _catalog.ViewFactory.Release(effect);
        }

        private bool TryGetShipEntity(out Entity shipEntity)
        {
            var query = _entityManager.CreateEntityQuery(typeof(ShipTag));
            if (query.CalculateEntityCount() > 0)
            {
                shipEntity = query.GetSingletonEntity();
                return true;
            }

            shipEntity = Entity.Null;
            return false;
        }

        private void OnRotateAction(float direction)
        {
            if (TryGetShipEntity(out var entity))
            {
                var rotateData = _entityManager.GetComponentData<RotateData>(entity);
                rotateData.TargetDirection = direction;
                _entityManager.SetComponentData(entity, rotateData);
            }
        }

        private void OnTrust(bool isTrust)
        {
            if (TryGetShipEntity(out var entity))
            {
                var thrustData = _entityManager.GetComponentData<ThrustData>(entity);
                thrustData.IsActive = isTrust;
                _entityManager.SetComponentData(entity, thrustData);
            }
        }

        private void OnAttack()
        {
            if (TryGetShipEntity(out var entity))
            {
                var gunData = _entityManager.GetComponentData<ECS.GunData>(entity);
                var rotateData = _entityManager.GetComponentData<RotateData>(entity);
                var moveData = _entityManager.GetComponentData<MoveData>(entity);

                gunData.Shooting = true;
                gunData.Direction = rotateData.Rotation;
                gunData.ShootPosition = moveData.Position;
                _entityManager.SetComponentData(entity, gunData);
            }
        }

        private void OnLaser()
        {
            if (TryGetShipEntity(out var entity))
            {
                var laserData = _entityManager.GetComponentData<LaserData>(entity);
                var rotateData = _entityManager.GetComponentData<RotateData>(entity);
                var moveData = _entityManager.GetComponentData<MoveData>(entity);

                laserData.Shooting = true;
                laserData.Direction = rotateData.Rotation;
                laserData.ShootPosition = moveData.Position;
                _entityManager.SetComponentData(entity, laserData);
            }
        }

        private void OnRocket()
        {
            if (TryGetShipEntity(out var entity))
            {
                var rocketData = _entityManager.GetComponentData<RocketData>(entity);
                var rotateData = _entityManager.GetComponentData<RotateData>(entity);
                var moveData = _entityManager.GetComponentData<MoveData>(entity);

                rocketData.Shooting = true;
                rocketData.Direction = rotateData.Rotation;
                rocketData.ShootPosition = moveData.Position;
                _entityManager.SetComponentData(entity, rocketData);
            }
        }
    }
}
