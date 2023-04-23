using System;
using System.Globalization;
using Model.Components;
using SelStrom.Asteroids.Configs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public class Game
    {
        private readonly PlayerInput _playerInput;
        private readonly GameData _configs;
        private readonly Model _model;

        private ShipModel _shipModel;
        private readonly EntitiesCatalog _catalog;
        private readonly GameScreen _gameScreen;

        public Game(EntitiesCatalog catalog, Model model, GameData configs, PlayerInput playerInput,
            GameScreen gameScreen)
        {
            _gameScreen = gameScreen;
            _model = model;
            _configs = configs;
            _playerInput = playerInput;
            _catalog = catalog;

            // TODO @a.shatalov: refactor
            _model.OnEntityDestroyed += OnEntityDestroyed;
        }

        public void Start()
        {
            _shipModel = _catalog.CreateShip(OnShipCollided);
            _shipModel.Gun.OnShooting = OnUserGunShooting;
            _shipModel.Laser.OnShooting = OnUserLaserShooting;

            for (var i = 0; i < _configs.AsteroidInitialCount; i++)
            {
                SpawnAsteroid(_shipModel.Move.Position.Value);
            }

            _playerInput.OnAttackAction += OnAttack;
            _playerInput.OnRotateAction += OnRotateAction;
            _playerInput.OnTrustAction += OnTrust;
            _playerInput.OnLaserAction += OnLaser;

            _model.ActionScheduler.ScheduleAction(SpawnNewEnemy, _configs.SpawnNewEnemyDurationSec);

            _gameScreen.Connect(new GameScreenData
            {
                ShipModel = _shipModel,
                Model = _model
            });
            _gameScreen.ToggleState(GameScreen.State.Game);
        }

        private void Stop()
        {
            _model.ActionScheduler.ResetSchedule();

            _playerInput.OnAttackAction -= OnAttack;
            _playerInput.OnRotateAction -= OnRotateAction;
            _playerInput.OnTrustAction -= OnTrust;
            _playerInput.OnLaserAction -= OnLaser;

            _gameScreen.ToggleState(GameScreen.State.EndGame);

            _playerInput.OnRestartAction += OnRestart;
        }

        private void OnRestart()
        {
            _playerInput.OnRestartAction -= OnRestart;
            _model.CleanUp();
            Start();
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
            var position = GameUtils.GetRandomUfoPosition(shipPosition, _model.GameArea, _configs.SpawnAllowedRadius);
            _catalog.CreateUfo(_shipModel, position, Random.insideUnitCircle.normalized, OnUfoCollided, OnEnemyGunShooting);
        }

        private void SpawnBigUfo(Vector2 shipPosition)
        {
            var position = GameUtils.GetRandomUfoPosition(shipPosition, _model.GameArea, _configs.SpawnAllowedRadius);
            _catalog.CreateBigUfo(_shipModel, position,
                (Random.insideUnitCircle * new Vector2(1, 0.1f)).normalized,
                OnUfoCollided, OnEnemyGunShooting);
        }

        private void SpawnAsteroid(Vector2 shipPosition)
        {
            var asteroidPosition =
                GameUtils.GetRandomAsteroidPosition(shipPosition, _model.GameArea, _configs.SpawnAllowedRadius);
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
        }

        private void OnUserBulletCollided(BulletModel bullet, Collision2D col)
        {
            Kill(bullet);
            col.otherCollider.enabled = false;

            // TODO @a.shatalov: impl score receiver
            if (_catalog.TryFindModel<AsteroidModel, AsteroidVisual>(col.gameObject, out var asteroidModel))
            {
                _model.ReceiveScore(asteroidModel);
                Kill(asteroidModel);
            }

            if (_catalog.TryFindModel<UfoBigModel, UfoVisual>(col.gameObject, out var ufoModel))
            {
                _model.ReceiveScore(ufoModel);
            }
        }
        
        private void OnEnemyBulletCollided(BulletModel bullet, Collision2D col)
        {
            Kill(bullet);
            col.otherCollider.enabled = false;
        }

        private void PlayEffect(GameObject prefab, in Vector2 position)
        {
            // TODO @a.shatalov: cleanup effect
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
            switch (entityModel)
            {
                case AsteroidModel asteroidModel:
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
                    break;
                case ShipModel shipModel:
                    PlayEffect(_configs.VfxBlowPrefab, shipModel.Move.Position.Value);
                    break;
                case UfoBigModel ufoModel:
                    PlayEffect(_configs.VfxBlowPrefab, ufoModel.Move.Position.Value);
                    break;
            }
        }

        private void OnEnemyGunShooting(GunComponent gun)
        {
            _catalog.CreateBullet(_configs.Bullet, _configs.Bullet.EnemyPrefab, gun.ShootPosition, gun.Direction,
                OnEnemyBulletCollided);
        }

        private void OnUserGunShooting(GunComponent gun)
        {
            _catalog.CreateBullet(_configs.Bullet, _configs.Bullet.Prefab, gun.ShootPosition, gun.Direction,
                OnUserBulletCollided);
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
                    _model.ReceiveScore(model);
                    Kill(model);
                }
            }
        }


        private void OnRotateAction(float direction)
        {
            _shipModel.Rotate.TargetDirection = direction;
        }

        private void OnTrust(bool isTrust)
        {
            _shipModel.Thrust.IsActive.Value = isTrust;
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
    }
}