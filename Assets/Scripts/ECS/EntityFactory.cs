using Unity.Entities;
using Unity.Mathematics;

namespace SelStrom.Asteroids.ECS
{
    public static class EntityFactory
    {
        public static Entity CreateShip(
            EntityManager em,
            float2 position,
            float moveSpeed,
            float thrustAcceleration,
            float thrustMaxSpeed,
            int gunMaxShoots,
            float gunReloadSec,
            int laserMaxShoots,
            float laserReloadSec,
            int missileMaxShoots,
            float missileReloadSec)
        {
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new ShipTag());
            em.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = moveSpeed,
                Direction = new float2(1f, 0f)
            });
            em.AddComponentData(entity, new RotateData
            {
                Rotation = new float2(1f, 0f),
                TargetDirection = 0f
            });
            em.AddComponentData(entity, new ThrustData
            {
                UnitsPerSecond = thrustAcceleration,
                MaxSpeed = thrustMaxSpeed,
                IsActive = false
            });
            em.AddComponentData(entity, new GunData
            {
                MaxShoots = gunMaxShoots,
                ReloadDurationSec = gunReloadSec,
                CurrentShoots = gunMaxShoots,
                ReloadRemaining = gunReloadSec
            });
            em.AddComponentData(entity, new LaserData
            {
                MaxShoots = laserMaxShoots,
                UpdateDurationSec = laserReloadSec,
                CurrentShoots = laserMaxShoots,
                ReloadRemaining = laserReloadSec
            });
            em.AddComponentData(entity, new MissileData
            {
                MaxShoots = missileMaxShoots,
                ReloadDurationSec = missileReloadSec,
                CurrentShoots = missileMaxShoots,
                ReloadRemaining = missileReloadSec
            });
            return entity;
        }

        public static Entity CreateAsteroid(
            EntityManager em,
            float2 position,
            float speed,
            float2 direction,
            int age,
            int score)
        {
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new AsteroidTag());
            em.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            em.AddComponentData(entity, new AgeData
            {
                Age = age
            });
            em.AddComponentData(entity, new ScoreValue
            {
                Score = score
            });
            return entity;
        }

        public static Entity CreateBullet(
            EntityManager em,
            float2 position,
            float speed,
            float2 direction,
            float lifeTime,
            bool isPlayer)
        {
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new BulletTag());
            em.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            em.AddComponentData(entity, new LifeTimeData
            {
                TimeRemaining = lifeTime
            });
            if (isPlayer)
            {
                em.AddComponentData(entity, new PlayerBulletTag());
            }
            else
            {
                em.AddComponentData(entity, new EnemyBulletTag());
            }
            return entity;
        }

        public static Entity CreateUfoBig(
            EntityManager em,
            float2 position,
            float speed,
            float2 direction,
            int gunMaxShoots,
            float gunReloadSec,
            int score)
        {
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new UfoBigTag());
            em.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            em.AddComponentData(entity, new GunData
            {
                MaxShoots = gunMaxShoots,
                ReloadDurationSec = gunReloadSec,
                CurrentShoots = gunMaxShoots,
                ReloadRemaining = gunReloadSec
            });
            em.AddComponentData(entity, new ShootToData());
            em.AddComponentData(entity, new ScoreValue
            {
                Score = score
            });
            return entity;
        }

        public static Entity CreateMissile(
            EntityManager em,
            float2 position,
            float speed,
            float2 direction,
            float lifeTime,
            float turnSpeed)
        {
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new MissileTag());
            em.AddComponentData(entity, new PlayerMissileTag());
            em.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            em.AddComponentData(entity, new LifeTimeData
            {
                TimeRemaining = lifeTime
            });
            em.AddComponentData(entity, new HomingData
            {
                TurnSpeed = turnSpeed
            });
            return entity;
        }

        public static Entity CreateUfo(
            EntityManager em,
            float2 position,
            float speed,
            float2 direction,
            int gunMaxShoots,
            float gunReloadSec,
            float moveToEvery,
            int score)
        {
            var entity = em.CreateEntity();
            em.AddComponentData(entity, new UfoTag());
            em.AddComponentData(entity, new MoveData
            {
                Position = position,
                Speed = speed,
                Direction = direction
            });
            em.AddComponentData(entity, new GunData
            {
                MaxShoots = gunMaxShoots,
                ReloadDurationSec = gunReloadSec,
                CurrentShoots = gunMaxShoots,
                ReloadRemaining = gunReloadSec
            });
            em.AddComponentData(entity, new ShootToData());
            em.AddComponentData(entity, new MoveToData
            {
                Every = moveToEvery,
                ReadyRemaining = moveToEvery
            });
            em.AddComponentData(entity, new ScoreValue
            {
                Score = score
            });
            return entity;
        }
    }
}
