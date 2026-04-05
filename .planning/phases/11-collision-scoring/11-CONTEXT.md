# Phase 11: Collision & Scoring - Context

**Gathered:** 2026-04-05 (auto mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

Ракета взаимодействует с игровым миром -- уничтожает врагов при столкновении, начисляет очки и уничтожается сама. Чистая ECS-логика коллизий без визуала. Покрытие EditMode юнит-тестами.

</domain>

<decisions>
## Implementation Decisions

### Обработка коллизии ракеты
- **D-01:** Ракета обрабатывается по паттерну PlayerBullet -- при столкновении с врагом оба получают DeadTag, плюс начисление ScoreValue с enemy entity
- **D-02:** Добавить новую ветку в `ProcessCollision`: `IsRocket(entityA) && IsEnemy(entityB)` (и зеркальный вариант), аналогично существующей `IsPlayerBullet && IsEnemy`
- **D-03:** Новый helper `IsRocket` проверяет наличие `RocketTag` компонента (уже создан в Phase 10)

### Начисление очков
- **D-04:** ScoreValue используется как есть -- очки за уничтожение врага ракетой идентичны очкам за уничтожение пулей (ScoreValue привязан к enemy entity, не к типу снаряда)

### Scope коллизий ракеты
- **D-05:** Ракета коллидирует ТОЛЬКО с врагами (AsteroidTag, UfoBigTag, UfoTag) -- не с кораблём, не с пулями, не с другими ракетами
- **D-06:** При столкновении с ЛЮБЫМ врагом (включая случайные на пути к цели) ракета уничтожается (DeadTag)

### Дробление астероидов
- **D-07:** Ракета вызывает дробление астероидов -- DeadTag на астероиде обрабатывается существующей системой дробления (DeadEntityCleanupSystem/Bridge), ракета не вносит изменений в логику дробления

### Тестирование
- **D-08:** Тесты коллизий ракеты добавляются в существующий `CollisionHandlerTests.cs` -- новые test methods по аналогии с `PlayerBulletHitsAsteroid_BothGetDeadTag` и `PlayerBulletHitsAsteroid_ScoreIncreased`
- **D-09:** Минимальный набор тестов: Rocket+Asteroid (DeadTag, Score), Rocket+Ufo (DeadTag, Score), Rocket+UfoBig (DeadTag, Score), а также негативные: Rocket+Ship (нет коллизии), Rocket+PlayerBullet (нет коллизии)

### Claude's Discretion
- Порядок проверок в ProcessCollision (до или после существующих веток PlayerBullet)
- Нужен ли helper CreateRocketEntity в AsteroidsEcsTestFixture или использовать прямое создание entity
- Формулировка сообщений Assert

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` -- COLL-01, COLL-02, COLL-03 -- коллизия ракеты с астероидами, UFO, самоуничтожение

### Collision system (primary target)
- `Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs` -- система обработки коллизий, добавить ветки для RocketTag
- `Assets/Scripts/ECS/Components/CollisionEventData.cs` -- структура событий коллизий
- `Assets/Scripts/ECS/Components/ScoreData.cs` -- ScoreData singleton + ScoreValue component
- `Assets/Scripts/ECS/Components/Tags/DeadTag.cs` -- маркер уничтожения entity

### Rocket components (from Phase 10)
- `Assets/Scripts/ECS/Components/Tags/RocketTag.cs` -- тег ракеты (уже создан)
- `Assets/Scripts/ECS/Components/RocketTargetData.cs` -- данные цели ракеты

### Enemy tags
- `Assets/Scripts/ECS/Components/Tags/AsteroidTag.cs` -- тег астероида
- `Assets/Scripts/ECS/Components/Tags/UfoBigTag.cs` -- тег большого UFO
- `Assets/Scripts/ECS/Components/Tags/UfoTag.cs` -- тег малого UFO

### Test patterns
- `Assets/Tests/EditMode/ECS/CollisionHandlerTests.cs` -- существующие тесты коллизий, сюда добавлять ракетные тесты
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` -- базовый fixture с helper-методами

### Prior phase context
- `.planning/phases/10-ecs-core/10-CONTEXT.md` -- решения Phase 10 по ECS-компонентам ракеты

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EcsCollisionHandlerSystem.ProcessCollision` -- добавить 2 ветки (Rocket+Enemy, зеркальная), полностью аналогичные PlayerBullet+Enemy
- `IsEnemy` helper -- уже проверяет AsteroidTag/UfoBigTag/UfoTag, переиспользуется для ракеты
- `MarkDead` helper -- уже добавляет DeadTag, переиспользуется
- `AddScore` helper -- уже читает ScoreValue и прибавляет к ScoreData, переиспользуется
- `CollisionHandlerTests` -- тестовый fixture с SetUp (ScoreData singleton, CollisionEvent buffer), helper AddCollisionEvent, RunSystem

### Established Patterns
- Коллизии обрабатываются через DynamicBuffer<CollisionEventData> singleton -- событие содержит EntityA + EntityB
- ProcessCollision проверяет пары (A,B) и (B,A) зеркально для каждого типа коллизии
- IsX() helpers проверяют наличие Tag-компонента через `em.HasComponent<T>(entity)`
- Тесты: создать entity, добавить CollisionEvent, RunSystem, проверить DeadTag/Score

### Integration Points
- `EcsCollisionHandlerSystem.ProcessCollision` -- единственная точка изменения продакшн-кода
- `CollisionHandlerTests` -- единственная точка добавления тестов
- Bridge-слой (Phase 12) подхватит DeadTag ракеты автоматически через DeadEntityCleanupSystem

</code_context>

<specifics>
## Specific Ideas

No specific requirements -- open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None -- discussion stayed within phase scope

</deferred>

---

*Phase: 11-collision-scoring*
*Context gathered: 2026-04-05*
