using UnityEngine;
using UnityEngine.InputSystem;

namespace SelStrom.Asteroids
{
    public class InputHelper : MonoBehaviour
    {
        private GameController _gameController;

        public void Connect(GameController gameController)
        {
            _gameController = gameController;
        }
        
        private void OnAttack()
        {
            _gameController.ShipShoot();
        }

        private void OnRotate(InputValue inputValue)
        {
            _gameController.ShipRotate(inputValue.Get<float>());
        }

        private void OnAccelerate(InputValue inputValue)
        {
            _gameController.ShipThrust(inputValue.isPressed);
        }

        private void OnLaser()
        {
            Debug.Log("do OnLaser");
        }
    }
}