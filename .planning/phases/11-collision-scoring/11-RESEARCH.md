# Phase 11: Collision & Scoring - Research

**Researched:** 2026-04-05
**Domain:** ECS Collision handling -- расширение EcsCollisionHandlerSystem для ракет
**Confidence:** HIGH

## Summary

Фаза 11 -- минимальное расширение существующей системы коллизий. Требуется добавить 2 зеркальных ветки `IsRocket && IsEnemy` в `ProcessCollision`, helper `IsRocket`, и написать 7-9 тестов по существующему паттерну. Никаких новых компонентов, систем или архитектурных решений не нужно.

Вся инфраструктура уже создана в Phase 10: `RocketTag`, `CreateRocketEntity` helper в тестовом fixture, паттерн коллизий с `MarkDead` + `AddScore`. Фаза сводится к копированию паттерна PlayerBullet+Enemy с заменой `IsPlayerBullet` на `IsRocket`.

**Primary recommendation:** Добавить ветки Rocket+Enemy в ProcessCollision сразу после PlayerBullet+Enemy, написать тесты по образцу существующих.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Ракета обрабатывается по паттерну PlayerBullet -- при столкновении с врагом оба получают DeadTag, плюс начисление ScoreValue с enemy entity
- **D-02:** Добавить новую ветку в `ProcessCollision`: `IsRocket(entityA) && IsEnemy(entityB)` (и зеркальный вариант), аналогично существующей `IsPlayerBullet && IsEnemy`
- **D-03:** Новый helper `IsRocket` проверяет наличие `RocketTag` компонента (уже создан в Phase 10)
- **D-04:** ScoreValue используется как есть -- очки за уничтожение врага ракетой идентичны очкам за уничтожение пулей (ScoreValue привязан к enemy entity, не к типу снаряда)
- **D-05:** Ракета коллидирует ТОЛЬКО с врагами (AsteroidTag, UfoBigTag, UfoTag) -- не с кораблём, не с пулями, не с другими ракетами
- **D-06:** При столкновении с ЛЮБЫМ врагом (включая случайные на пути к цели) ракета уничтожается (DeadTag)
- **D-07:** Ракета вызывает дробление астероидов -- DeadTag на астероиде обрабатывается существующей системой дробления, ракета не вносит изменений в логику дробления
- **D-08:** Тесты коллизий ракеты добавляются в существующий `CollisionHandlerTests.cs`
- **D-09:** Минимальный набор тестов: Rocket+Asteroid (DeadTag, Score), Rocket+Ufo (DeadTag, Score), Rocket+UfoBig (DeadTag, Score), Rocket+Ship (нет коллизии), Rocket+PlayerBullet (нет коллизии)

### Claude's Discretion
- Порядок проверок в ProcessCollision (до или после существующих веток PlayerBullet)
- Нужен ли helper CreateRocketEntity в AsteroidsEcsTestFixture или использовать прямое создание entity
- Формулировка сообщений Assert

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| COLL-01 | Ракета уничтожает астероиды при столкновении и начисляет очки | Ветка `IsRocket && IsEnemy` с `MarkDead` + `AddScore` -- полный паттерн PlayerBullet |
| COLL-02 | Ракета уничтожает UFO при столкновении и начисляет очки | Тот же паттерн -- `IsEnemy` уже покрывает UfoTag и UfoBigTag |
| COLL-03 | Ракета уничтожается при столкновении с любым врагом | `MarkDead` на обеих entities в ветке Rocket+Enemy |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- Язык документации и ответов: русский
- Фигурные скобки обязательны даже для однострочных блоков (K&R стиль по контексту файла)
- Однострочники запрещены
- Namespace: `SelStrom.Asteroids.ECS` для компонентов и систем
- Namespace тестов: `SelStrom.Asteroids.Tests.EditMode.ECS`
- При багфиксе -- обязателен регрессионный тест

## Standard Stack

Новых зависимостей нет. Фаза использует только существующие компоненты и системы.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Unity.Entities | из packages-lock.json | ECS EntityManager, ISystem, IComponentData | Основа ECS-архитектуры проекта [VERIFIED: codebase] |
| Unity.Collections | из packages-lock.json | NativeArray для копирования событий | Используется в ProcessCollision [VERIFIED: codebase] |
| Unity.Mathematics | из packages-lock.json | float2 для позиций в тестах | Используется в fixture [VERIFIED: codebase] |
| NUnit | 1.1.33 (test-framework) | Юнит-тесты EditMode | Стандарт Unity тестирования [VERIFIED: codebase] |

## Architecture Patterns

### Паттерн ProcessCollision -- зеркальные ветки

**What:** Каждый тип коллизии проверяется дважды -- (A,B) и (B,A) -- поскольку порядок entities в CollisionEventData не детерминирован.

**Существующая структура ProcessCollision:** [VERIFIED: EcsCollisionHandlerSystem.cs]
```
1. PlayerBullet + Enemy  (и зеркальная)
2. EnemyBullet + Ship    (и зеркальная)
3. Ship + Enemy          (и зеркальная)
4. Asteroid + UfoAny     (и зеркальная)
```

**Рекомендация:** Добавить Rocket+Enemy ПОСЛЕ PlayerBullet+Enemy (позиция 2), ДО EnemyBullet+Ship. Логическая группировка: сначала все "оружие игрока vs враги", потом "оружие врагов vs игрок", потом остальное.

### Паттерн Helper-метод для type check

```csharp
// Source: EcsCollisionHandlerSystem.cs -- существующий паттерн
private bool IsRocket(ref EntityManager em, Entity entity)
{
    return em.HasComponent<RocketTag>(entity);
}
```

### Паттерн теста коллизий

```csharp
// Source: CollisionHandlerTests.cs -- существующий паттерн
[Test]
public void RocketHitsAsteroid_BothGetDeadTag()
{
    var rocket = CreateRocketEntity(
        float2.zero, 15f, new float2(1f, 0f), 5f);
    var asteroid = CreateAsteroidEntity(
        new float2(5f, 0f), 3f, new float2(-1f, 0f), 3, score: 100);

    AddCollisionEvent(rocket, asteroid);
    RunSystem();

    Assert.IsTrue(m_Manager.HasComponent<DeadTag>(rocket),
        "Rocket should get DeadTag on collision with asteroid");
    Assert.IsTrue(m_Manager.HasComponent<DeadTag>(asteroid),
        "Asteroid should get DeadTag on collision with rocket");
}
```

### Anti-Patterns to Avoid
- **Дублирование логики MarkDead/AddScore:** Не копировать логику, а вызывать существующие helper-методы
- **Забыть зеркальную ветку:** Каждая проверка ОБЯЗАТЕЛЬНО с (A,B) и (B,A)
- **ScoreValue на ракете:** Ракета НЕ имеет ScoreValue -- очки берутся с enemy entity (D-04)

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Проверка типа entity | Инлайн `em.HasComponent<RocketTag>` в ProcessCollision | Helper `IsRocket` | Единообразие с IsPlayerBullet, IsEnemy и др. |
| Создание rocket entity в тестах | Ручное создание через m_Manager | `CreateRocketEntity` из fixture | Уже создан в Phase 10, переиспользовать |

## Common Pitfalls

### Pitfall 1: Забытая зеркальная проверка
**What goes wrong:** Коллизия работает только при определенном порядке entities в событии
**Why it happens:** CollisionEventData не гарантирует порядок EntityA/EntityB
**How to avoid:** Всегда добавлять обе ветки -- (Rocket,Enemy) и (Enemy,Rocket)
**Warning signs:** Тест с reversed order (AddCollisionEvent(enemy, rocket)) не проходит

### Pitfall 2: Early return мешает обработке
**What goes wrong:** Если добавить ветку Rocket+Enemy после return в другой ветке, она никогда не выполнится
**Why it happens:** ProcessCollision использует `return` после каждого match
**How to avoid:** Каждая пара -- отдельный if-блок с return, порядок проверок важен только для производительности
**Warning signs:** Нет -- все проверки независимы при правильном порядке

### Pitfall 3: DeadTag уже есть
**What goes wrong:** AddComponent<DeadTag> на entity, который уже мёртв
**Why it happens:** Двойная коллизия в одном кадре
**How to avoid:** `MarkDead` уже проверяет `!em.HasComponent<DeadTag>` [VERIFIED: codebase line 153-157]
**Warning signs:** Нет -- уже защищено

## Code Examples

### Продакшн-код: ветки Rocket+Enemy в ProcessCollision

```csharp
// Source: паттерн из EcsCollisionHandlerSystem.cs lines 58-73
// Добавить ПОСЛЕ PlayerBullet+Enemy, ДО EnemyBullet+Ship

// Rocket + Enemy (Asteroid/Ufo/UfoBig)
if (IsRocket(ref em, entityA) && IsEnemy(ref em, entityB))
{
    MarkDead(ref em, entityA);
    MarkDead(ref em, entityB);
    AddScore(ref em, entityB, ref scoreData);
    return;
}

if (IsRocket(ref em, entityB) && IsEnemy(ref em, entityA))
{
    MarkDead(ref em, entityB);
    MarkDead(ref em, entityA);
    AddScore(ref em, entityA, ref scoreData);
    return;
}
```

### Helper IsRocket

```csharp
// Source: паттерн IsPlayerBullet из EcsCollisionHandlerSystem.cs line 119-122
private bool IsRocket(ref EntityManager em, Entity entity)
{
    return em.HasComponent<RocketTag>(entity);
}
```

### Тест: негативный -- Rocket не коллидирует с Ship

```csharp
// Source: паттерн NoCollisionEvents_NothingHappens из CollisionHandlerTests.cs
[Test]
public void RocketHitsShip_NoDeadTag()
{
    var rocket = CreateRocketEntity(
        float2.zero, 15f, new float2(1f, 0f), 5f);
    var ship = CreateShipEntity(new float2(5f, 0f), 0f);

    AddCollisionEvent(rocket, ship);
    RunSystem();

    Assert.IsFalse(m_Manager.HasComponent<DeadTag>(rocket),
        "Rocket should NOT get DeadTag on collision with ship");
    Assert.IsFalse(m_Manager.HasComponent<DeadTag>(ship),
        "Ship should NOT get DeadTag on collision with rocket");
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.1.33 (NUnit) |
| Config file | `Assets/Tests/EditMode/SelStrom.Asteroids.Tests.EditMode.asmdef` |
| Quick run command | `Unity -runTests -testPlatform EditMode -testFilter CollisionHandlerTests` |
| Full suite command | `Unity -runTests -testPlatform EditMode` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| COLL-01 | Rocket+Asteroid: DeadTag + Score | unit | `CollisionHandlerTests.RocketHitsAsteroid_BothGetDeadTag` | Wave 0 |
| COLL-01 | Rocket+Asteroid: Score | unit | `CollisionHandlerTests.RocketHitsAsteroid_ScoreIncreased` | Wave 0 |
| COLL-02 | Rocket+Ufo: DeadTag + Score | unit | `CollisionHandlerTests.RocketHitsUfo_BothDeadAndScoreIncreased` | Wave 0 |
| COLL-02 | Rocket+UfoBig: DeadTag + Score | unit | `CollisionHandlerTests.RocketHitsUfoBig_BothDeadAndScoreIncreased` | Wave 0 |
| COLL-03 | Rocket уничтожается при коллизии | unit | Покрыто тестами COLL-01/02 (проверка DeadTag на ракете) | Wave 0 |
| COLL-03 | Reversed order | unit | `CollisionHandlerTests.RocketHitsAsteroid_ReversedOrder_BothGetDeadTag` | Wave 0 |
| - | Negative: Rocket+Ship (нет коллизии) | unit | `CollisionHandlerTests.RocketHitsShip_NoDeadTag` | Wave 0 |
| - | Negative: Rocket+PlayerBullet (нет коллизии) | unit | `CollisionHandlerTests.RocketHitsPlayerBullet_NoDeadTag` | Wave 0 |

### Wave 0 Gaps
None -- `CollisionHandlerTests.cs` и `AsteroidsEcsTestFixture.cs` уже существуют, `CreateRocketEntity` уже в fixture. Все тесты добавляются в существующий файл.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| - | - | - | - |

**Все утверждения верифицированы** по исходному коду проекта. Таблица пуста -- подтверждение пользователя не требуется.

## Open Questions

Нет открытых вопросов. Все решения зафиксированы в CONTEXT.md, паттерн полностью ясен из кодовой базы.

**Из STATE.md (Blockers):**
- "Physics Layer 'Rocket': решить -- отдельный layer или переиспользовать PlayerBullet" -- это вопрос Physics/Bridge слоя (Phase 12), НЕ Phase 11. Phase 11 работает на уровне ECS entities, физические слои не затрагивает.
- "Ракета + вражеская пуля: ракета неуязвима или уничтожается?" -- решено в D-05: ракета коллидирует ТОЛЬКО с врагами, не с пулями.

## Sources

### Primary (HIGH confidence)
- `Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs` -- текущая система коллизий, точка изменения
- `Assets/Tests/EditMode/ECS/CollisionHandlerTests.cs` -- существующие тесты, паттерн для новых
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` -- fixture с CreateRocketEntity (Phase 10)
- `Assets/Scripts/ECS/Components/Tags/RocketTag.cs` -- тег ракеты (Phase 10)
- `Assets/Scripts/ECS/Components/ScoreData.cs` -- ScoreData + ScoreValue
- `Assets/Scripts/ECS/Components/CollisionEventData.cs` -- структура событий
- `.planning/phases/11-collision-scoring/11-CONTEXT.md` -- решения пользователя

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- все компоненты верифицированы в кодовой базе
- Architecture: HIGH -- паттерн 1:1 копирует PlayerBullet, никаких новых решений
- Pitfalls: HIGH -- единственный реальный риск (забытая зеркальная ветка) хорошо задокументирован

**Research date:** 2026-04-05
**Valid until:** 2026-05-05 (стабильная кодовая база, нет внешних зависимостей)
