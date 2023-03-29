using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Vector2 = System.Numerics.Vector2;

namespace SelStrom.Asteroids
{
    public class InputHelper : MonoBehaviour
    {
        [SerializeField] public GameController GameController;
        private ShipModel _shipModel;
        
        private void OnAttack()
        {
            GameController.Shoot(_shipModel.Move.Position, _shipModel.Rotation);
        }

        private void OnRotate(InputValue inputValue)
        {
            _shipModel.RotationDirection = inputValue.Get<float>();
            // Debug.Log($"do OnRotate input: {_rotationDirection}");
        }

        private void OnAccelerate(InputValue inputValue)
        {
            _shipModel.IsAccelerated = inputValue.isPressed;
            // Debug.Log($"do OnAccelerate {inputValue.isPressed}");
        }

        private void OnLaser()
        {
            Debug.Log("do OnLaser");
        }

        public void Connect(ShipModel shipModel)
        {
            _shipModel = shipModel;
            // _spaceshipModel.Direction = new Vector2(1, 0);
        }
    }
}