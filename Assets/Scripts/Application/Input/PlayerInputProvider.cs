﻿using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SelStrom.Asteroids
{
    public class PlayerInputProvider : MonoBehaviour
    {
        public event Action OnAttackAction;
        public event Action<InputValue> OnRotateAction;
        public event Action<InputValue> OnTrustAction;
        public event Action OnLaserAction;
        public event Action OnBackAction;

        [PublicAPI]
        private void OnAttack()
        {
            OnAttackAction?.Invoke();
        }

        [PublicAPI]
        private void OnRotate(InputValue inputValue)
        {
            OnRotateAction?.Invoke(inputValue);
        }

        [PublicAPI]
        private void OnAccelerate(InputValue inputValue)
        {
            OnTrustAction?.Invoke(inputValue);
        }

        [PublicAPI]
        private void OnLaser()
        {
            OnLaserAction?.Invoke();
        }
        
        [PublicAPI]
        private void OnBack()
        {
            OnBackAction?.Invoke();
        }
    }
}