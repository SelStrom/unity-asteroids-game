using Unity.Collections;
using Unity.Entities;

namespace SelStrom.Asteroids.ECS
{
    public partial struct EcsCollisionHandlerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScoreData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            // Find the singleton entity with CollisionEventData buffer
            Entity bufferEntity = Entity.Null;
            foreach (var (buffer, entity) in
                     SystemAPI.Query<DynamicBuffer<CollisionEventData>>().WithEntityAccess())
            {
                bufferEntity = entity;
                break;
            }

            if (bufferEntity == Entity.Null)
            {
                return;
            }

            var events = em.GetBuffer<CollisionEventData>(bufferEntity);
            if (events.Length == 0)
            {
                return;
            }

            // Копируем события перед обработкой, т.к. structural changes (AddComponent)
            // инвалидируют DynamicBuffer
            var eventsCopy = events.ToNativeArray(Allocator.Temp);
            events.Clear();

            var scoreEntity = SystemAPI.GetSingletonEntity<ScoreData>();
            var scoreData = em.GetComponentData<ScoreData>(scoreEntity);

            for (int i = 0; i < eventsCopy.Length; i++)
            {
                ProcessCollision(ref em, eventsCopy[i].EntityA, eventsCopy[i].EntityB,
                    ref scoreData);
            }

            em.SetComponentData(scoreEntity, scoreData);
            eventsCopy.Dispose();
        }

        private void ProcessCollision(
            ref EntityManager em, Entity entityA, Entity entityB, ref ScoreData scoreData)
        {
            // PlayerBullet + Enemy (Asteroid/Ufo/UfoBig)
            if (IsPlayerBullet(ref em, entityA) && IsEnemy(ref em, entityB))
            {
                MarkDead(ref em, entityA);
                MarkDead(ref em, entityB);
                AddScore(ref em, entityB, ref scoreData);
                return;
            }

            if (IsPlayerBullet(ref em, entityB) && IsEnemy(ref em, entityA))
            {
                MarkDead(ref em, entityB);
                MarkDead(ref em, entityA);
                AddScore(ref em, entityA, ref scoreData);
                return;
            }

            // EnemyBullet + Ship
            if (IsEnemyBullet(ref em, entityA) && IsShip(ref em, entityB))
            {
                MarkDead(ref em, entityA);
                MarkDead(ref em, entityB);
                return;
            }

            if (IsEnemyBullet(ref em, entityB) && IsShip(ref em, entityA))
            {
                MarkDead(ref em, entityB);
                MarkDead(ref em, entityA);
                return;
            }

            // Ship + Enemy (Asteroid/Ufo/UfoBig)
            if (IsShip(ref em, entityA) && IsEnemy(ref em, entityB))
            {
                MarkDead(ref em, entityA);
                return;
            }

            if (IsShip(ref em, entityB) && IsEnemy(ref em, entityA))
            {
                MarkDead(ref em, entityB);
                return;
            }
        }

        private bool IsPlayerBullet(ref EntityManager em, Entity entity)
        {
            return em.HasComponent<PlayerBulletTag>(entity);
        }

        private bool IsEnemyBullet(ref EntityManager em, Entity entity)
        {
            return em.HasComponent<EnemyBulletTag>(entity);
        }

        private bool IsShip(ref EntityManager em, Entity entity)
        {
            return em.HasComponent<ShipTag>(entity);
        }

        private bool IsEnemy(ref EntityManager em, Entity entity)
        {
            return em.HasComponent<AsteroidTag>(entity)
                   || em.HasComponent<UfoBigTag>(entity)
                   || em.HasComponent<UfoTag>(entity);
        }

        private void MarkDead(ref EntityManager em, Entity entity)
        {
            if (!em.HasComponent<DeadTag>(entity))
            {
                em.AddComponent<DeadTag>(entity);
            }
        }

        private void AddScore(ref EntityManager em, Entity enemyEntity, ref ScoreData scoreData)
        {
            if (em.HasComponent<ScoreValue>(enemyEntity))
            {
                scoreData.Value += em.GetComponentData<ScoreValue>(enemyEntity).Score;
            }
        }
    }
}
