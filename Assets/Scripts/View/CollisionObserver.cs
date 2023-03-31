using System;
using UnityEngine;

namespace SelStrom.Asteroids
{
    public class CollisionObserver : BaseView<ShipModel>
    {
        /*
        private void Awake()
        {
            Debug.Log("[ColliderObserver][Awake] calling");
        }
        */

        /*private void OnEnable()
        {
            // Debug.Log("[ColliderObserver][OnEnable] calling");
        }*/

        private void OnCollisionEnter2D(Collision2D col)
        {
            Debug.Log($"[ColliderObserver][OnCollisionEnter2D] calling go {gameObject.name} to {col.gameObject.name}");
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Debug.Log("[ColliderObserver][OnCollisionStay2D] calling");
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            // Debug.Log("[ColliderObserver][OnCollisionExit2D] calling");
        }
    }
}