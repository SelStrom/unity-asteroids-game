using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SelStrom.Asteroids
{
    public static class GameUtils
    {
        public static Vector2 GetRandomUfoPosition(in Vector2 shipPosition, in Vector2 gameArea,
            int spawnAllowedRadius)
        {
            var position = new Vector2(0, Random.Range(0, gameArea.y)) - gameArea * 0.5f;

            var verticalDistance = shipPosition.y - position.y;
            var allowedDistance = verticalDistance - spawnAllowedRadius;
            if (allowedDistance < 0)
            {
                position.y += verticalDistance / Math.Abs(verticalDistance) * allowedDistance;
            }

            return position;
        }

        public static Vector2 GetRandomAsteroidPosition(in Vector2 shipPosition, in Vector2 gameArea,
            int spawnAllowedRadius)
        {
            var position = new Vector2(Random.Range(0, gameArea.x), Random.Range(0, gameArea.y)) - gameArea * 0.5f;

            var distance = shipPosition - position;
            var allowedDistance = distance.magnitude - spawnAllowedRadius;
            if (allowedDistance < 0)
            {
                position += distance.normalized * allowedDistance;
            }

            return position;
        }
    }
}