using Shtl.Mvvm;
using TMPro;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ScoreViewModel : AbstractViewModel {
        public readonly ReactiveValue<string> Score = new();
    }

    public class ScoreVisual : AbstractWidgetView<ScoreViewModel>
    {
        [SerializeField] private TMP_Text _scoreText = default;

        protected override void OnConnected()
        {
            Bind.From(ViewModel.Score).To(_scoreText);
        }
    }
}