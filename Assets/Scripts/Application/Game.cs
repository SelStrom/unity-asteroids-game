using System;
using System.Globalization;
using Model.Components;
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
        private Hud _hud;

        public Game(EntitiesCatalog catalog, Model model, GameData configs, PlayerInputProvider playerInput, Hud hud)
        {
            _hud = hud;
            _model = model;
            _configs = configs;
            _playerInput = playerInput;
            _catalog = catalog;
        }

        public void Start()
        {
            _model.OnEntityDestroyed += OnEntityDestroyed;

            _shipModel = _catalog.CreateShip(OnShipCollided);
            _shipModel.Gun.OnShooting += OnUserGunShooting;
            _shipModel.Laser.OnShooting += OnUserLaserShooting;

            for (var i = 0; i < _configs.AsteroidInitialCount; i++)
            {
                SpawnAsteroid(_shipModel.Move.Position.Value);
            }
            
            _playerInput.OnAttackAction += OnAttack;
            _playerInput.OnRotateAction += OnRotateAction;
            _playerInput.OnTrustAction += OnTrust;
            _playerInput.OnLaserAction += OnLaser;

            _model.ActionScheduler.ScheduleAction(SpawnNewEnemy, _configs.SpawnNewEnemyDurationSec);
            
            InitializeHud();
        }

        private void InitializeHud()
        {
            _hud.Connect(new HudData());
            _shipModel.Move.Position.OnChanged += OnShipPositionChanged;
            _shipModel.Move.Speed.OnChanged += OnShipSpeedChanged;
            _shipModel.Rotate.Rotation.OnChanged += OnShipRotationChanged;
            _shipModel.Laser.CurrentShoots.OnChanged += OnCurrentShootsChanged;
            _shipModel.Laser.ReloadRemaining.OnChanged += OnReloadRemainingChanged;

            OnShipPositionChanged(_shipModel.Move.Position.Value);
            OnShipSpeedChanged(_shipModel.Move.Speed.Value);
            OnShipRotationChanged(_shipModel.Rotate.Rotation.Value);
            OnCurrentShootsChanged(_shipModel.Laser.CurrentShoots.Value);
            OnReloadRemainingChanged(_shipModel.Laser.ReloadRemaining.Value);
        }

        private void OnReloadRemainingChanged(float timeRemaining)
        {
            _hud.Data.LaserReloadTime.Value = $"Reload laser: {TimeSpan.FromSeconds((int)timeRemaining):%s} sec";
        }

        private void OnCurrentShootsChanged(int shoots)
        {
            _hud.Data.LaserShootCount.Value = $"Laser shoots: {shoots.ToString()}";
            _hud.Data.LaserReloadTimeVisible.Value = shoots < _configs.Laser.LaserMaxShoots;
        }

        private void OnShipRotationChanged(Vector2 direction)
        {
            _hud.Data.RotationAngle.Value = $"Rotation: {(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg).ToString("F1", CultureInfo.InvariantCulture)} degrees";
        }

        private void OnShipPositionChanged(Vector2 position)
        {
            _hud.Data.Coordinates.Value = $"Coordinates: {position.ToString("F1")}";
        }
        
        private void OnShipSpeedChanged(float speed)
        {
            _hud.Data.Speed.Value = $"Speed: {speed.ToString("F1", CultureInfo.InvariantCulture) } points/sec";
        }

        public void Stop()
        {
            _playerInput.OnAttackAction -= OnAttack;
            _playerInput.OnRotateAction -= OnRotateAction;
            _playerInput.OnTrustAction -= OnTrust;
            _playerInput.OnLaserAction -= OnLaser;

            _model.ActionScheduler.ResetSchedule();
        }

        private void SpawnNewEnemy()
        {
            var index = Random.Range(0, 3);
            switch (index)
            {
                case 0:
                    SpawnAsteroid(_shipModel.Move.Position.Value);
                    break;
                case 1:
                    SpawnUfo(_shipModel.Move.Position.Value);
                    break;
                case 2:
                    SpawnBigUfo(_shipModel.Move.Position.Value);
                    break;
            }

            _model.ActionScheduler.ScheduleAction(SpawnNewEnemy, _configs.SpawnNewEnemyDurationSec);
        }

        private void SpawnUfo(Vector2 shipPosition)
        {
            var position = GetRandomUfoPosition(shipPosition, _model.GameArea, _configs.SpawnAllowedRadius);
            var ufo = _catalog.CreateUfo(_shipModel, position, Random.insideUnitCircle.normalized, OnUfoCollided);
            ufo.Gun.OnShooting = OnEnemyShooting;
        }

        private void SpawnBigUfo(Vector2 shipPosition)
        {
            var position = GetRandomUfoPosition(shipPosition, _model.GameArea, _configs.SpawnAllowedRadius);
            var ufo = _catalog.CreateBigUfo(_shipModel, position, (Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized,
                OnUfoCollided);
            ufo.Gun.OnShooting = OnEnemyShooting;
        }

        private void SpawnAsteroid(Vector2 shipPosition)
        {
            var asteroidPosition =
                GetRandomAsteroidPosition(shipPosition, _model.GameArea, _configs.SpawnAllowedRadius);
            _catalog.CreateAsteroid(3, asteroidPosition, Random.Range(1f, 3f));
        }

        private void OnUfoCollided(UfoBigModel model)
        {
            Kill(model);
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

            if (_catalog.TryFindModel<AsteroidModel, AsteroidVisual>(col.gameObject, out var asteroidModel))
            {
                Kill(asteroidModel);
            }
        }

        private void PlayEffect(GameObject prefab, in Vector2 position)
        {
            var effect = _catalog.ViewFactory.Get<EffectVisual>(prefab);
            effect.transform.position = position;
            effect.Connect(OnEffectStopped);
        }

        private void OnEffectStopped(EffectVisual effect)
        {
            _catalog.ViewFactory.Release(effect);
        }

        private void OnEntityDestroyed(IGameEntityModel entityModel)
        {
            _catalog.Release(entityModel);
        }

        private void Kill(IGameEntityModel entityModel)
        {
            entityModel.Kill();

            if (entityModel is AsteroidModel asteroidModel)
            {
                PlayEffect(_configs.VfxBlowPrefab, asteroidModel.Move.Position.Value);

                var age = asteroidModel.Age - 1;
                if (age <= 0)
                {
                    return;
                }

                var position = asteroidModel.Move.Position.Value;
                var speed = Math.Min(asteroidModel.Move.Speed.Value * 2, 10f);
                _catalog.CreateAsteroid(age, position, speed);
                _catalog.CreateAsteroid(age, position, speed);

                return;
            }

            if (entityModel is UfoBigModel ufoModel)
            {
                PlayEffect(_configs.VfxBlowPrefab, ufoModel.Move.Position.Value);
            }

            if (entityModel is ShipModel shipModel)
            {
                PlayEffect(_configs.VfxBlowPrefab, shipModel.Move.Position.Value);
            }
        }

        private void OnEnemyShooting(GunComponent gun)
        {
            _catalog.CreateBullet(_configs.Bullet, _configs.Bullet.EnemyPrefab, gun.ShootPosition, gun.Direction,
                OnBulletCollided);
        }

        private void OnUserGunShooting(GunComponent gun)
        {
            _catalog.CreateBullet(_configs.Bullet, _configs.Bullet.Prefab, gun.ShootPosition, gun.Direction,
                OnBulletCollided);
        }

        private void OnUserLaserShooting(LaserComponent laserComponent)
        {
            var effect = _catalog.ViewFactory.Get<LineRenderer>(_configs.Laser.Prefab);
            effect.transform.position = _shipModel.Move.Position.Value;
            var direction = _shipModel.Laser.Direction;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effect.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            _model.ActionScheduler.ScheduleAction(() => { _catalog.ViewFactory.Release(effect.gameObject); },
                _configs.Laser.BeamEffectLifetimeSec);

            var hits = new RaycastHit2D[30];
            var size = Physics2D.RaycastNonAlloc(_shipModel.Move.Position.Value, _shipModel.Rotate.Rotation.Value, hits,
                _model.GameArea.magnitude, LayerMask.GetMask("Asteroid", "Enemy"));
            if (size <= 0)
            {
                return;
            }

            for (var i = 0; i < size; i++)
            {
                var hit = hits[i];
                var gameObject = hit.collider.gameObject;
                if (_catalog.TryFindModel<IGameEntityModel, BaseVisual>(gameObject, out var model))
                {
                    Kill(model);
                }
            }
        }


        private void OnRotateAction(InputValue inputValue)
        {
            _shipModel.Rotate.TargetDirection = inputValue.Get<float>();
        }

        private void OnTrust(InputValue inputValue)
        {
            _shipModel.Thrust.IsActive.Value = inputValue.isPressed;
        }

        private void OnAttack()
        {
            _shipModel.Gun.Shooting = true;
            _shipModel.Gun.Direction = _shipModel.Rotate.Rotation.Value;
            _shipModel.Gun.ShootPosition = _shipModel.ShootPoint;
        }

        private void OnLaser()
        {
            _shipModel.Laser.Shooting = true;
            _shipModel.Laser.Direction = _shipModel.Rotate.Rotation.Value;
            _shipModel.Laser.ShootPosition = _shipModel.ShootPoint;
        }


        private static Vector2 GetRandomUfoPosition(in Vector2 shipPosition, in Vector2 gameArea, int spawnAllowedRadius)
        {
            var position = new Vector2(0, Random.Range(0, gameArea.y)) - gameArea * 0.5f;

            var verticalDistance = shipPosition.y - position.y;
            var allowedDistance = verticalDistance - spawnAllowedRadius;
            if (allowedDistance < 0)
            {
                position.y += verticalDistance / Math.Abs(verticalDistance) * allowedDistance;
            }

            return position;
        }

        private static Vector2 GetRandomAsteroidPosition(in Vector2 shipPosition, in Vector2 gameArea, int spawnAllowedRadius)
        {
            var position = new Vector2(Random.Range(0, gameArea.x), Random.Range(0, gameArea.y)) - gameArea * 0.5f;

            var distance = shipPosition - position;
            var allowedDistance = distance.magnitude - spawnAllowedRadius;
            if (allowedDistance < 0)
            {
                position += distance.normalized * allowedDistance;
            }

            return position;
        }
    }
}