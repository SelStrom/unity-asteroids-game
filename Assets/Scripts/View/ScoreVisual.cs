using System;
using System.Collections.Generic;
using Shtl.Mvvm;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SelStrom.Asteroids
{
    public class ScoreViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<string> Score = new();
        public readonly ReactiveValue<bool> IsNameInputVisible = new();
        public readonly ReactiveValue<bool> IsLeaderboardVisible = new();
        public readonly ReactiveValue<bool> IsLoadingVisible = new();
        public readonly ReactiveValue<string> PlayerRankText = new();
        public readonly ReactiveValue<bool> IsPlayerRankVisible = new();
        public readonly ReactiveValue<string> DefaultPlayerName = new();
        public readonly ReactiveValue<Action> ChangeNameAction = new();
        public readonly ReactiveValue<bool> IsChangeNameVisible = new();
        public readonly ReactiveList<LeaderboardEntryViewModel> Entries = new();
        public readonly ReactiveValue<Action> OnSubmitAction = new();
        public readonly ReactiveValue<Action> OnRestartAction = new();
    }

    public class ScoreVisual : AbstractWidgetView<ScoreViewModel>
    {
        [SerializeField] private TMP_Text _scoreText;

        [Header("Name Input")]
        [SerializeField] private GameObject _nameInputContainer;
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private Button _submitButton;

        [Header("Leaderboard")]
        [SerializeField] private GameObject _leaderboardContainer;
        [SerializeField] private LeaderboardEntryVisual _entryTemplate;
        [SerializeField] private Transform _entriesContainer;
        [SerializeField] private TMP_Text _playerRankText;
        [SerializeField] private Button _changeNameButton;

        [Header("Loading")]
        [SerializeField] private GameObject _loadingIndicator;

        [SerializeField] private Button _restartButton;

        private readonly List<LeaderboardEntryVisual> _entries = new();

        public string PlayerName => _nameInput != null ? _nameInput.text : "";

        protected override void OnConnected()
        {
            ClearEntryWidgets();

            Bind.From(ViewModel.Score).To(_scoreText);

            Bind.From(_submitButton).To(ViewModel.OnSubmitAction);
            Bind.From(ViewModel.IsNameInputVisible).To(_nameInputContainer);
            Bind.From(ViewModel.IsLeaderboardVisible).To(_leaderboardContainer);
            Bind.From(ViewModel.IsLoadingVisible).To(_loadingIndicator);
            Bind.From(ViewModel.PlayerRankText).To(_playerRankText);
            Bind.From(ViewModel.IsPlayerRankVisible).To(_playerRankText.gameObject);
            Bind.From(ViewModel.Entries).To(_entries, _entryTemplate, _entriesContainer);
            Bind.From(_changeNameButton).To(ViewModel.ChangeNameAction);
            Bind.From(ViewModel.IsChangeNameVisible).To(_changeNameButton.gameObject);
            Bind.From(_restartButton).To(ViewModel.OnRestartAction);

            if (_nameInput != null)
            {
                _nameInput.text = ViewModel.DefaultPlayerName.Value ?? "";
            }
        }

        private void ClearEntryWidgets()
        {
            foreach (var entry in _entries)
            {
                if (entry != null)
                {
                    Destroy(entry.gameObject);
                }
            }
            _entries.Clear();
        }
    }
}
