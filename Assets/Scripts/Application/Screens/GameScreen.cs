using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SelStrom.Asteroids.Configs;
using SelStrom.Asteroids.ECS;
using Shtl.Mvvm;
using Unity.Entities;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public struct GameScreenData
    {
        public Game Game;
    }

    public sealed class GameScreen : AbstractScreen
    {
        private const string PlayerNameKey = "PlayerName";

        public enum State
        {
            Default = 0,
            Game,
            EndGame
        }

        private readonly HudVisual _hudVisual;
        private readonly ScoreVisual _score;
        private readonly GameData _configs;
        private readonly LeaderboardService _leaderboardService;
        private readonly MonoBehaviour _coroutineHost;
        private HudData _hudData;

        private State _currentState;

        private GameScreenData _data;

        public GameScreen(HudVisual hudVisual, ScoreVisual score, GameData configs,
            LeaderboardService leaderboardService, MonoBehaviour coroutineHost)
        {
            _hudVisual = hudVisual;
            _score = score;
            _configs = configs;
            _leaderboardService = leaderboardService;
            _coroutineHost = coroutineHost;
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

            var world = World.DefaultGameObjectInjectionWorld;
            var bridge = world.GetExistingSystemManaged<ObservableBridgeSystem>();
            if (bridge != null)
            {
                bridge.SetHudData(_hudData);
                bridge.SetLaserMaxShoots(_configs.Laser.LaserMaxShoots);
                bridge.SetRocketMaxShoots(_configs.Rocket.MaxRockets);
            }
        }

        private void DeactivateHud()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var bridge = world.GetExistingSystemManaged<ObservableBridgeSystem>();
                if (bridge != null)
                {
                    bridge.ClearReferences();
                }
            }

            CleanUp();
            _hudData = null;
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

        private int ReadScoreFromEcs()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var em = world.EntityManager;
                var query = em.CreateEntityQuery(typeof(ScoreData));
                if (query.CalculateEntityCount() > 0)
                {
                    return em.GetComponentData<ScoreData>(query.GetSingletonEntity()).Value;
                }
            }

            return 0;
        }

        private void ShowEndGame()
        {
            var score = _data.Game.GetCurrentScore();

            var scoreVm = new ScoreViewModel();
            scoreVm.Score.Value = $"score: {score}";
            scoreVm.IsNameInputVisible.Value = false;
            scoreVm.IsLeaderboardVisible.Value = false;
            scoreVm.IsLoadingVisible.Value = true;
            scoreVm.IsPlayerRankVisible.Value = false;
            scoreVm.IsChangeNameVisible.Value = false;
            scoreVm.OnSubmitAction.Value = OnSubmitClicked;
            scoreVm.ChangeNameAction.Value = OnChangeNameClicked;
            scoreVm.OnRestartAction.Value = OnRestartClicked;

            _score.Connect(scoreVm);
            _hudVisual.gameObject.SetActive(false);
            _score.gameObject.SetActive(true);

            var savedName = PlayerPrefs.GetString(PlayerNameKey, "");
            if (!string.IsNullOrEmpty(savedName))
            {
                SubmitAndShowLeaderboard(savedName, score);
            }
            else
            {
                FetchAndShowLeaderboard();
            }
        }

        private void OnRestartClicked()
        {
            _data.Game.Restart();
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

            var score = _data.Game.GetCurrentScore();
            SubmitAndShowLeaderboard(playerName, score);
        }

        private void OnChangeNameClicked()
        {
            var viewModel = _score.ViewModel;
            viewModel.DefaultPlayerName.Value = PlayerPrefs.GetString(PlayerNameKey, "");
            viewModel.IsChangeNameVisible.Value = false;
            viewModel.IsNameInputVisible.Value = true;
        }

        private void FetchAndShowLeaderboard()
        {
            _coroutineHost.StartCoroutine(FetchAndShowLeaderboardRoutine());
        }

        private IEnumerator FetchAndShowLeaderboardRoutine()
        {
            var viewModel = _score.ViewModel;

            var result = new CoroutineResult<List<LeaderboardEntry>>();
            yield return _leaderboardService.GetTopScores(result);

            if (_score.ViewModel != viewModel)
            {
                yield break;
            }

            if (!result.IsSuccess)
            {
                Debug.LogError($"[LeaderboardService] Failed to fetch leaderboard: {result.Error}");
                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsNameInputVisible.Value = true;
                yield break;
            }

            PopulateLeaderboard(viewModel, result.Value, null);
            viewModel.IsLoadingVisible.Value = false;
            viewModel.IsLeaderboardVisible.Value = true;
            viewModel.IsNameInputVisible.Value = true;
        }

        private void SubmitAndShowLeaderboard(string playerName, int score)
        {
            _coroutineHost.StartCoroutine(SubmitAndShowLeaderboardRoutine(playerName, score));
        }

        private IEnumerator SubmitAndShowLeaderboardRoutine(string playerName, int score)
        {
            var viewModel = _score.ViewModel;
            viewModel.IsLoadingVisible.Value = true;

            var playerResult = new CoroutineResult<LeaderboardEntry?>();
            yield return _leaderboardService.GetPlayerScore(playerResult);

            var bestScore = Math.Max(playerResult.Value?.Score ?? 0, score);

            var submitResult = new CoroutineResult();
            yield return _leaderboardService.SubmitScore(playerName, bestScore, submitResult);

            if (_score.ViewModel != viewModel)
            {
                yield break;
            }

            if (!submitResult.IsSuccess)
            {
                Debug.LogError($"[LeaderboardService] Failed: {submitResult.Error}");
                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsNameInputVisible.Value = true;
                yield break;
            }

            var topResult = new CoroutineResult<List<LeaderboardEntry>>();
            yield return _leaderboardService.GetTopScores(topResult);

            if (_score.ViewModel != viewModel)
            {
                yield break;
            }

            if (!topResult.IsSuccess)
            {
                Debug.LogError($"[LeaderboardService] Failed: {topResult.Error}");
                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsNameInputVisible.Value = true;
                yield break;
            }

            playerResult = new CoroutineResult<LeaderboardEntry?>();
            yield return _leaderboardService.GetPlayerScore(playerResult);

            if (_score.ViewModel != viewModel)
            {
                yield break;
            }

            if (!playerResult.IsSuccess)
            {
                Debug.LogError($"[LeaderboardService] Failed: {playerResult.Error}");
                viewModel.IsLoadingVisible.Value = false;
                viewModel.IsNameInputVisible.Value = true;
                yield break;
            }

            viewModel.Entries.Clear();
            PopulateLeaderboard(viewModel, topResult.Value, playerResult.Value);

            viewModel.IsLoadingVisible.Value = false;
            viewModel.IsLeaderboardVisible.Value = true;
            viewModel.IsChangeNameVisible.Value = true;
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
