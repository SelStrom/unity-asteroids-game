using System;
using System.Globalization;
using SelStrom.Asteroids.Configs;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public struct GameScreenData
    {
        public ShipModel ShipModel;
        public Model Model;
    }
    
    public class GameScreen
    {
        private readonly HudVisual _hudVisual;
        private readonly ScoreVisual _score;
        private readonly GameData _configs;
        private HudData _hudData;
        
        private GameScreenData _data;

        public GameScreen(HudVisual hudVisual, ScoreVisual score, GameData configs)
        {
            _hudVisual = hudVisual;
            _score = score;
            _configs = configs;
        }

        public void Connect(in GameScreenData data)
        {
            _data = data;
        }

        private void ActivateHud()
        {
            _hudData = new HudData();
            var shipModel = _data.ShipModel;
            shipModel.Move.Position.OnChanged += OnShipPositionChanged;
            shipModel.Move.Speed.OnChanged += OnShipSpeedChanged;
            shipModel.Rotate.Rotation.OnChanged += OnShipRotationChanged;
            shipModel.Laser.CurrentShoots.OnChanged += OnCurrentShootsChanged;
            shipModel.Laser.ReloadRemaining.OnChanged += OnReloadRemainingChanged;

            _hudVisual.Connect(_hudData);
            OnShipPositionChanged(shipModel.Move.Position.Value);
            OnShipSpeedChanged(shipModel.Move.Speed.Value);
            OnShipRotationChanged(shipModel.Rotate.Rotation.Value);
            OnCurrentShootsChanged(shipModel.Laser.CurrentShoots.Value);
            OnReloadRemainingChanged(shipModel.Laser.ReloadRemaining.Value);
        }

        private void DeactivateHud()
        {
            var shipModel = _data.ShipModel;
            shipModel.Move.Position.OnChanged -= OnShipPositionChanged;
            shipModel.Move.Speed.OnChanged -= OnShipSpeedChanged;
            shipModel.Rotate.Rotation.OnChanged -= OnShipRotationChanged;
            shipModel.Laser.CurrentShoots.OnChanged -= OnCurrentShootsChanged;
            shipModel.Laser.ReloadRemaining.OnChanged -= OnReloadRemainingChanged;
            _hudData = null;
        }

        private void OnReloadRemainingChanged(float timeRemaining)
        {
            _hudData.LaserReloadTime.Value = $"Reload laser: {TimeSpan.FromSeconds((int)timeRemaining):%s} sec";
        }

        private void OnCurrentShootsChanged(int shoots)
        {
            _hudData.LaserShootCount.Value = $"Laser shoots: {shoots.ToString()}";
            _hudData.LaserReloadTimeVisible.Value = shoots < _configs.Laser.LaserMaxShoots;
        }

        private void OnShipRotationChanged(Vector2 direction)
        {
            _hudData.RotationAngle.Value =
                $"Rotation: {(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg).ToString("F1", CultureInfo.InvariantCulture)} degrees";
        }

        private void OnShipPositionChanged(Vector2 position)
        {
            _hudData.Coordinates.Value = $"Coordinates: {position.ToString("F1")}";
        }

        private void OnShipSpeedChanged(float speed)
        {
            _hudData.Speed.Value = $"Speed: {speed.ToString("F1", CultureInfo.InvariantCulture)} points/sec";
        }

        public void ToggleState(State state)
        {
            switch (state)
            {
                case State.Game:
                    ActivateHud();
                    _hudVisual.gameObject.SetActive(true);
                    _score.gameObject.SetActive(false);
                    break;
                case State.EndGame:
                    DeactivateHud();
                    _score.Connect(_data.Model.Score);
                    _hudVisual.gameObject.SetActive(false);
                    _score.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        public enum State
        {
            Game,
            EndGame
        }
    }
}