using TMPro;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class ScoreVisual : BaseVisual<int>
    {
        [SerializeField] private TextMeshProUGUI _scoreText = default;

        protected override void OnConnected()
        {
            _scoreText.text = $"score: {Data}";
        }
    }
}