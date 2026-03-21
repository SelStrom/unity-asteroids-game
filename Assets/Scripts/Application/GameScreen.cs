using System;
using System.Collections.Generic;
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
        private const string PlayerNameKey = "PlayerName";

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
        private readonly LeaderboardService _leaderboardService;
        private HudData _hudData;
        
        private State _currentState;
        
        private GameScreenData _data;

        public GameScreen(HudVisual hudVisual, ScoreVisual score, GameData configs,
            LeaderboardService leaderboardService)
        {
            _hudVisual = hudVisual;
            _score = score;
            _configs = configs;
            _leaderboardService = leaderboardService;
        }

        public void Connect(in GameScreenData data)
        {
            _data = data;
            _leaderboardService.Initialize();
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
                    ShowEndGame();
                    break;
                case State.Default:
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void ShowEndGame()
        {
            var scoreVm = new ScoreViewModel();
            scoreVm.Score.Value = $"score: {_data.Model.Score}";
            scoreVm.IsNameInputVisible.Value = false;
            scoreVm.IsLeaderboardVisible.Value = false;
            scoreVm.IsLoadingVisible.Value = true;
            scoreVm.IsPlayerRankVisible.Value = false;
            scoreVm.IsChangeNameVisible.Value = false;
            scoreVm.SubmitAction.Value = OnSubmitClicked;
            scoreVm.ChangeNameAction.Value = OnChangeNameClicked;

            _score.Connect(scoreVm);
            _hudVisual.gameObject.SetActive(false);
            _score.gameObject.SetActive(true);

            var savedName = PlayerPrefs.GetString(PlayerNameKey, "");
            if (!string.IsNullOrEmpty(savedName))
            {
                SubmitAndShowLeaderboard(savedName, _data.Model.Score);
            }
            else
            {
                FetchAndShowLeaderboard();
            }
        }

        private void OnSubmitClicked()
        {
            var playerName = _score.PlayerName.Trim();
            if (string.IsNullOrEmpty(playerName))
            {
                return;
            }

            PlayerPrefs.SetString(PlayerNameKey, playerName);
            PlayerPrefs.Save();

            var viewModel = _score.ViewModel;
            viewModel.IsNameInputVisible.Value = false;
            viewModel.IsLoadingVisible.Value = true;

            SubmitAndShowLeaderboard(playerName, _data.Model.Score);
        }

        private void OnChangeNameClicked()
        {
            var viewModel = _score.ViewModel;
            viewModel.DefaultPlayerName.Value = PlayerPrefs.GetString(PlayerNameKey, "");
            viewModel.IsChangeNameVisible.Value = false;
            viewModel.IsNameInputVisible.Value = true;
        }

        private async void FetchAndShowLeaderboard()
        {
            var viewModel = _score.ViewModel;

            try
            {
                var topScores = await _leaderboardService.GetTopScoresAsync();

                if (_score.ViewModel != viewModel)
                {
                    return;
                }

                PopulateLeaderboard(viewModel, topScores, null);

                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsLeaderboardVisible.Value = true;
                viewModel.IsNameInputVisible.Value = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] Failed to fetch leaderboard: {e}");

                if (_score.ViewModel != viewModel)
                {
                    return;
                }

                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsNameInputVisible.Value = true;
            }
        }

        private async void SubmitAndShowLeaderboard(string playerName, int score)
        {
            var viewModel = _score.ViewModel;
            viewModel.IsLoadingVisible.Value = true;

            try
            {
                await _leaderboardService.SubmitScoreAsync(playerName, score);

                if (_score.ViewModel != viewModel)
                {
                    return;
                }

                var topScores = await _leaderboardService.GetTopScoresAsync();

                if (_score.ViewModel != viewModel)
                {
                    return;
                }

                var playerEntry = await _leaderboardService.GetPlayerScoreAsync();

                if (_score.ViewModel != viewModel)
                {
                    return;
                }

                viewModel.Entries.Clear();
                PopulateLeaderboard(viewModel, topScores, playerEntry);

                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsLeaderboardVisible.Value = true;
                viewModel.IsChangeNameVisible.Value = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] Failed: {e}");

                if (_score.ViewModel != viewModel)
                {
                    return;
                }

                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsNameInputVisible.Value = true;
            }
        }

        private static void PopulateLeaderboard(ScoreViewModel viewModel,
            List<LeaderboardEntry> topScores, LeaderboardEntry? playerEntry)
        {
            var playerInTopTen = false;
            foreach (var entry in topScores)
            {
                var entryVm = new LeaderboardEntryViewModel();
                entryVm.Rank.Value = $"{entry.Rank}";
                entryVm.Name.Value = entry.PlayerName;
                entryVm.Score.Value = $"{entry.Score}";
                entryVm.NameColor.Value = entry.IsCurrentPlayer ? Color.yellow : Color.white;
                viewModel.Entries.Add(entryVm);

                if (entry.IsCurrentPlayer)
                {
                    playerInTopTen = true;
                }
            }

            if (!playerInTopTen && playerEntry.HasValue)
            {
                viewModel.PlayerRankText.Value =
                    $"#{playerEntry.Value.Rank}  {playerEntry.Value.PlayerName}  {playerEntry.Value.Score}";
                viewModel.IsPlayerRankVisible.Value = true;
            }
        }
    }
}
