using TMPro;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public sealed class GuiText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text = default;
        public string Text => _text.text;
    }
}