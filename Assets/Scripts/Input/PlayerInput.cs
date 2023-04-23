using System;
using JetBrains.Annotations;
using UnityEngine.InputSystem;

namespace SelStrom.Asteroids
{
    public class PlayerInput
    {
        private PlayerActions _actions;
        private PlayerActions.PlayerControlsActions _playerControls;

        public event Action OnAttackAction;
        public event Action<float> OnRotateAction;
        public event Action<bool> OnTrustAction;
        public event Action OnLaserAction;
        public event Action OnBackAction;
        public event Action OnRestartAction;

        public PlayerInput()
        {
            _actions = new PlayerActions();
            _playerControls = _actions.PlayerControls;
            _playerControls.Enable();
            
            _playerControls.Attack.performed += OnAttack;
            _playerControls.Rotate.performed += OnRotate;
            _playerControls.Rotate.canceled += OnRotate;
            _playerControls.Accelerate.performed += OnAccelerate;
            _playerControls.Accelerate.canceled += OnAccelerate;
            _playerControls.Laser.performed += OnLaser;
            _playerControls.Back.performed += OnBack;
            _playerControls.Restart.performed += OnRestart;
        }

        [PublicAPI]
        private void OnAttack(InputAction.CallbackContext _)
        {
            OnAttackAction?.Invoke();
        }

        [PublicAPI]
        private void OnRotate(InputAction.CallbackContext ctx)
        {
            OnRotateAction?.Invoke(ctx.ReadValue<float>());
        }

        [PublicAPI]
        private void OnAccelerate(InputAction.CallbackContext ctx)
        {
            OnTrustAction?.Invoke(ctx.performed);
        }

        [PublicAPI]
        private void OnLaser(InputAction.CallbackContext _)
        {
            OnLaserAction?.Invoke();
        }
        
        [PublicAPI]
        private void OnBack(InputAction.CallbackContext _)
        {
            OnBackAction?.Invoke();
        }
        
        [PublicAPI]
        private void OnRestart(InputAction.CallbackContext _)
        {
            OnRestartAction?.Invoke();
        }
    }
}