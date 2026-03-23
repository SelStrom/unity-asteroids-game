using System;

namespace SelStrom.Asteroids {
    public sealed class TitleScreen : AbstractScreen
    {
        private readonly TitleScreenView _view;

        public TitleScreen(TitleScreenView view)
        {
            _view = view;
        }

        public void Connect(Action onStart)
        {
            _view.Connect(new TitleScreenViewModel {
                OnStartAction = { Value = () => {
                    _view.gameObject.SetActive(false);
                    CleanUp();
                    onStart.Invoke();
                } }
            });
        }
    }
}