using Shtl.Mvvm;
using TMPro;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class LeaderboardEntryViewModel : AbstractViewModel
    {
        public readonly ReactiveValue<string> Rank = new();
        public readonly ReactiveValue<string> Name = new();
        public readonly ReactiveValue<string> Score = new();
        public readonly ReactiveValue<Color> NameColor = new(Color.white);
    }

    public class LeaderboardEntryVisual : AbstractWidgetView<LeaderboardEntryViewModel>
    {
        [SerializeField] private TMP_Text _rank;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _score;

        protected override void OnConnected()
        {
            Bind.From(ViewModel.Rank).To(_rank);
            Bind.From(ViewModel.Name).To(_name);
            Bind.From(ViewModel.Score).To(_score);
            Bind.From(ViewModel.NameColor).To(_name);
        }
    }
}
