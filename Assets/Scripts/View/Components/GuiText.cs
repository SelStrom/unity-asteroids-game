using TMPro;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public sealed class GuiText : BaseVisual<ObservableField<string>>
    {
        [SerializeField] private TextMeshProUGUI _text = default;

        protected override void OnConnected()
        {
            Data.OnChanged += OnChanged;
            OnChanged(Data.Value);
        }

        private void OnChanged(string text)
        {
            _text.text = text;
        }

        protected override void OnDisposed()
        {
            Data.OnChanged -= OnChanged;
            base.OnConnected();
        }
    }
}