using System;
using System.Collections.Generic;
using System.IO;
using SelStrom.Asteroids.Configs;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class Game
    {
        private readonly PlayerInputProvider _playerInput;
        private readonly GameData _configs;
        private readonly Model _model;

        private ShipModel _shipModel;
        private readonly EntitiesCatalog _catalog;

        public Game(EntitiesCatalog catalog, Model model, GameData configs, PlayerInputProvider playerInput)
        {
            _model = model;
            _configs = configs;
            _playerInput = playerInput;
            _catalog = catalog;
        }

        public void Start()
        {
            _model.OnEntityDestroyed += OnEntityDestroyed;

            _shipModel = _catalog.CreateShip(OnShipCollided);
            for (var i = 0; i < _configs.AsteroidInitialCount; i++)
            {
                SpawnAsteroid(_shipModel.Move.Position.Value);
            }

            _playerInput.OnAttackAction += OnAttack;
            _playerInput.OnRotateAction += OnRotateAction;
            _playerInput.OnTrustAction += OnTrust;
            _playerInput.OnLaserAction += OnLaser;

            _model.ScheduleAction(SpawnNewEnemy, _configs.SpawnNewEnemyDurationSec);
        }

        public void Stop()
        {
            _playerInput.OnAttackAction -= OnAttack;
            _playerInput.OnRotateAction -= OnRotateAction;
            _playerInput.OnTrustAction -= OnTrust;
            _playerInput.OnLaserAction -= OnLaser;

            _model.ResetSchedule();
        }

        private void SpawnNewEnemy()
        {
            SpawnAsteroid(_shipModel.Move.Position.Value);
            // TODO @a.shatalov: spawn ufo
            
            //...
            _model.ScheduleAction(SpawnNewEnemy, _configs.SpawnNewEnemyDurationSec);
        }

        private void SpawnAsteroid(Vector2 shipPosition)
        {
            var asteroidPosition = GetRandomAsteroidPosition(shipPosition, _model.GameArea, _configs.AsteroidSpawnAllowedRadius);
            _catalog.CreateAsteroid(3, asteroidPosition, Random.Range(1f, 3f));
        }

        private void OnShipCollided(Collision2D obj)
        {
            Kill(_shipModel);
            Stop();
            // TODO @a.shatalov: complete game;
        }

        private void OnBulletCollided(BulletModel bullet, Collision2D col)
        {
            Kill(bullet);

            col.otherCollider.enabled = false;
            var asteroidModel = _catalog.FindModel<AsteroidModel>(col.gameObject);
            Kill(asteroidModel);

            var age = asteroidModel.Age - 1;
            if (age <= 0)
            {
                return;
            }

            var position = asteroidModel.Move.Position.Value;
            var speed = Math.Min(asteroidModel.Move.Speed * 2, 10f);
            _catalog.CreateAsteroid(age, position, speed);
            _catalog.CreateAsteroid(age, position, speed);
        }

        private void OnEntityDestroyed(IGameEntityModel entityModel)
        {
            _catalog.Release(entityModel);
        }

        private static void Kill(IGameEntityModel entityModel)
        {
            entityModel.Kill();
        }
        
        private static Vector2 GetRandomAsteroidPosition(Vector2 shipPosition, Vector2 gameArea, int spawnAllowedRadius)
        {
            var asteroidPosition = new Vector2(Random.Range(0, gameArea.x), Random.Range(0, gameArea.y)) - gameArea / 2;

            var distance = shipPosition - asteroidPosition;
            var allowedDistance = distance.magnitude - spawnAllowedRadius;
            if (allowedDistance < 0)
            {
                asteroidPosition += distance.normalized * allowedDistance;
            }

            return asteroidPosition;
        }

        private void OnAttack()
        {
            _catalog.CreateBullet(_shipModel.ShootPoint, _shipModel.Rotate.Rotation.Value, OnBulletCollided);
        }

        private void OnRotateAction(InputValue inputValue)
        {
            _shipModel.Rotate.TargetDirection = inputValue.Get<float>();
        }

        private void OnTrust(InputValue inputValue)
        {
            _shipModel.Thrust.IsActive.Value = inputValue.isPressed;
        }
        
        private void OnLaser()
        {
            // TODO @a.shatalov: laser
        }
    }
}