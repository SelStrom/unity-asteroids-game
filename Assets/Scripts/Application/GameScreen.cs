using System;
using System.Globalization;
using SelStrom.Asteroids.Configs;
using Shtl.Mvvm;
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
        public enum State
        {
            Default = 0,
            Game,
            EndGame
        }

        private readonly EventBindingContext _context = new();
        private EventBindingContext Bind => _context;
        
        private readonly HudVisual _hudVisual;
        private readonly ScoreVisual _score;
        private readonly GameData _configs;
        private HudData _hudData;
        
        private State _currentState;
        
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
            _score.Connect(new ScoreViewModel());
        }

        private void ActivateHud()
        {
            _hudData = new HudData();
            _hudVisual.Connect(_hudData);
            
            var shipModel = _data.ShipModel;
            Bind.From(shipModel.Move.Position).To(OnShipPositionChanged);
            Bind.From(shipModel.Move.Speed).To(OnShipSpeedChanged);
            Bind.From(shipModel.Rotate.Rotation).To(OnShipRotationChanged);
            Bind.From(shipModel.Laser.CurrentShoots).To(OnCurrentShootsChanged);
            Bind.From(shipModel.Laser.ReloadRemaining).To(OnReloadRemainingChanged);

            Bind.InvokeAll();
        }

        private void DeactivateHud()
        {
            Bind.CleanUp();
            _hudData = null;
        }

        private void OnReloadRemainingChanged(float timeRemaining)
        {
            _hudData.LaserReloadTime.Value = $"Reload laser: {TimeSpan.FromSeconds((int)timeRemaining):%s} sec";
        }

        private void OnCurrentShootsChanged(int shoots)
        {
            _hudData.LaserShootCount.Value = $"Laser shoots: {shoots.ToString()}";
            _hudData.IsLaserReloadTimeVisible.Value = shoots < _configs.Laser.LaserMaxShoots;
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
            if (_currentState == state)
            {
                return;
            }

            _currentState = state;
            switch (_currentState)
            {
                case State.Game:
                    DeactivateHud();
                    ActivateHud();
                    _hudVisual.gameObject.SetActive(true);
                    _score.gameObject.SetActive(false);
                    break;
                case State.EndGame:
                    DeactivateHud();
                    _score.ViewModel.Score.Value = $"score: {_data.Model.Score}";
                    _hudVisual.gameObject.SetActive(false);
                    _score.gameObject.SetActive(true);
                    break;
                case State.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
            
        }
    }
}