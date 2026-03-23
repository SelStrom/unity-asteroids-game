using System;
using Shtl.Mvvm;
using UnityEngine;
using UnityEngine.UI;

namespace SelStrom.Asteroids {
    public class TitleScreenViewModel : AbstractViewModel {
        public readonly ReactiveValue<Action> OnStartAction = new();
    }
    
    public sealed class TitleScreenView : AbstractWidgetView<TitleScreenViewModel> {
        [SerializeField] private Button _startButton;
        protected override void OnConnected() => Bind.From(_startButton).To(ViewModel.OnStartAction);
    }
}