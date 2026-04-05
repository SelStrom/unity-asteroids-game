---
phase: 10-ecs-core
verified: 2026-04-05T22:15:00Z
status: human_needed
score: 5/5
gaps: []
deferred:
  - truth: "Боезапас на Ship entity уменьшается при запуске ракеты"
    addressed_in: "Phase 13"
    evidence: "Phase 13 goal: 'Игрок управляет запуском ракет -- нажатие R запускает ракету в игровом мире'. ROCK-01 mapped to Phase 13."
human_verification:
  - test: "Запустить EditMode-тесты RocketGuidanceSystemTests и RocketAmmoSystemTests в Unity Editor"
    expected: "Все 14 тестов (9 guidance + 5 ammo) GREEN"
    why_human: "Unity ECS тесты требуют запуск в Unity Editor (SystemBase, EntityManager, SystemAPI зависят от Unity runtime)"
  - test: "Запустить EntityFactoryTests в Unity Editor"
    expected: "Все тесты GREEN, включая CreateRocket_CreatesEntityWithAllComponents и CreateShip_HasRocketAmmoData"
    why_human: "Unity ECS EntityManager API недоступен вне Editor"
---

# Phase 10: ECS Core -- данные и логика ракет Verification Report

**Phase Goal:** Ракета существует как ECS-entity с полной логикой наведения, боезапаса и перезарядки
**Verified:** 2026-04-05T22:15:00Z
**Status:** human_needed
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | ECS-entity ракеты каждый кадр поворачивает Direction к ближайшему врагу с ограниченным turn rate | VERIFIED | EcsRocketGuidanceSystem.cs: SystemAPI.Query RefRW MoveData+RocketTargetData, RotateTowards с maxAngle=turnRate*deltaTime, FindClosestEnemy по 3 тегам |
| 2 | При уничтожении текущей цели (DeadTag) ракета переключается на следующую ближайшую цель | VERIFIED | EcsRocketGuidanceSystem.cs:22-26: EntityManager.Exists + HasComponent DeadTag check, сброс Target=Entity.Null, повторный FindClosestEnemy |
| 3 | Ракета самоуничтожается по истечении времени жизни (LifeTimeData) | VERIFIED | EntityFactory.CreateRocket (строка 170-172) добавляет LifeTimeData. Существующие EcsLifeTimeSystem + EcsDeadByLifeTimeSystem обрабатывают все entity с LifeTimeData |
| 4 | Боезапас на Ship entity восстанавливается инкрементально по таймеру | VERIFIED | EcsRocketAmmoSystem.cs: CurrentAmmo += 1 при ReloadRemaining <= 0, условие CurrentAmmo < MaxAmmo, сброс таймера |
| 5 | Все ECS-компоненты и системы покрыты EditMode юнит-тестами | VERIFIED | 16 тестов: RocketGuidanceSystemTests (9), RocketAmmoSystemTests (5), EntityFactoryTests CreateRocket + CreateShip_HasRocketAmmoData (2) |

**Score:** 5/5 truths verified

### Deferred Items

Items not yet met but explicitly addressed in later milestone phases.

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | Боезапас на Ship entity уменьшается при запуске ракеты | Phase 13 | Phase 13 goal: "Игрок управляет запуском ракет -- нажатие R запускает ракету в игровом мире". ROCK-01 mapped to Phase 13. |

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/ECS/Components/Tags/RocketTag.cs` | IComponentData маркер ракеты | VERIFIED | 6 строк, struct RocketTag : IComponentData, namespace SelStrom.Asteroids.ECS |
| `Assets/Scripts/ECS/Components/RocketTargetData.cs` | Данные цели + turn rate | VERIFIED | 10 строк, Entity Target + float TurnRateDegPerSec |
| `Assets/Scripts/ECS/Components/RocketAmmoData.cs` | Боезапас ракет на Ship | VERIFIED | 12 строк, MaxAmmo/CurrentAmmo/ReloadDurationSec/ReloadRemaining |
| `Assets/Scripts/ECS/EntityFactory.cs` | CreateRocket + расширенный CreateShip | VERIFIED | CreateRocket (6 params, строки 154-180), CreateShip с rocketMaxAmmo/rocketReloadSec (строки 18-19, 54-60) |
| `Assets/Scripts/ECS/Systems/EcsRocketGuidanceSystem.cs` | Система наведения | VERIFIED | 132 строки, partial class : SystemBase, UpdateAfter(EcsMoveSystem), FindClosestEnemy, RotateTowards |
| `Assets/Scripts/ECS/Systems/EcsRocketAmmoSystem.cs` | Система перезарядки | VERIFIED | 34 строки, partial struct : ISystem, UpdateAfter(EcsLaserSystem), инкрементальная перезарядка |
| `Assets/Tests/EditMode/ECS/RocketGuidanceSystemTests.cs` | Тесты наведения | VERIFIED | 279 строк, 9 [Test] методов, наследует AsteroidsEcsTestFixture |
| `Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs` | Тесты перезарядки | VERIFIED | 134 строки, 5 [Test] методов, наследует AsteroidsEcsTestFixture |
| `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` | CreateRocketEntity хелпер | VERIFIED | Строки 145-167, CreateRocketEntity + CreateShipEntity с RocketAmmoData |
| `Assets/Tests/EditMode/ECS/EntityFactoryTests.cs` | Тесты фабрики ракет | VERIFIED | CreateRocket_CreatesEntityWithAllComponents + CreateShip_HasRocketAmmoData |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| EntityFactory.CreateRocket | RocketTag + MoveData + LifeTimeData + RocketTargetData | AddComponentData | WIRED | Строки 163-178: 4 вызова em.AddComponentData с RocketTag, MoveData, LifeTimeData, RocketTargetData |
| EntityFactory.CreateShip | RocketAmmoData | AddComponentData | WIRED | Строки 54-60: em.AddComponentData(entity, new RocketAmmoData { ... }) |
| EcsRocketGuidanceSystem | MoveData.Direction | RefRW MoveData в SystemAPI.Query | WIRED | Строка 14: RefRW MoveData, строка 44: move.ValueRW.Direction = RotateTowards(...) |
| EcsRocketGuidanceSystem | RocketTargetData.Target | RefRW RocketTargetData | WIRED | Строки 20-32: target.ValueRO.Target проверка + target.ValueRW.Target = FindClosestEnemy |
| EcsRocketGuidanceSystem | DeadTag | HasComponent DeadTag | WIRED | Строка 23: EntityManager.HasComponent DeadTag(target). Строки 63,79,95: WithNone DeadTag в FindClosestEnemy |
| EcsRocketAmmoSystem | RocketAmmoData.CurrentAmmo | RefRW инкрементация | WIRED | Строка 28: ammo.ValueRW.CurrentAmmo += 1 |
| EcsRocketAmmoSystem | RocketAmmoData.ReloadRemaining | RefRW декремент | WIRED | Строка 24: ammo.ValueRW.ReloadRemaining -= deltaTime |

### Data-Flow Trace (Level 4)

Not applicable -- ECS systems are pure logic processors operating on component data. No rendering or UI output in this phase.

### Behavioral Spot-Checks

Step 7b: SKIPPED (Unity ECS тесты требуют Unity Editor runtime -- невозможно запустить из CLI)

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| ROCK-02 | 10-02 | Ракета автоматически наводится на ближайшего врага с ограниченным turn rate | SATISFIED | EcsRocketGuidanceSystem: FindClosestEnemy + RotateTowards с TurnRateDegPerSec |
| ROCK-03 | 10-02 | Ракета переключает цель при уничтожении текущей | SATISFIED | EcsRocketGuidanceSystem:20-26: DeadTag/Exists check -> target reset -> FindClosestEnemy |
| ROCK-04 | 10-01 | Ракета имеет ограниченное время жизни (LifeTimeData) | SATISFIED | EntityFactory.CreateRocket добавляет LifeTimeData с TimeRemaining |
| ROCK-05 | 10-01 | Боезапас ракет ограничен конфигурируемым количеством | SATISFIED | RocketAmmoData с MaxAmmo на Ship entity, CreateShip принимает rocketMaxAmmo |
| ROCK-06 | 10-03 | Ракеты респавнятся по таймеру (инкрементальная перезарядка) | SATISFIED | EcsRocketAmmoSystem: CurrentAmmo += 1 каждые ReloadDurationSec |
| TEST-01 | 10-02, 10-03 | Юнит-тесты на ECS-компоненты и системы ракет (EditMode) | SATISFIED | 16 тестов: 9 guidance + 5 ammo + 2 factory |

No orphaned requirements -- all 6 requirement IDs from REQUIREMENTS.md traceability (ROCK-02/03/04/05/06, TEST-01 mapped to Phase 10) accounted for in plans.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| EntitiesCatalog.cs | 109-110 | Hardcoded rocketMaxAmmo: 3, rocketReloadSec: 5f | Info | Временные значения до Phase 14 (CONF-01). Задокументировано в 10-01-SUMMARY.md |

### Human Verification Required

### 1. EditMode тесты системы наведения

**Test:** Открыть Unity Editor, запустить тесты в RocketGuidanceSystemTests (9 тестов)
**Expected:** Все 9 тестов GREEN (NoEnemies_DirectionUnchanged, SingleEnemy_TurnsTowardsEnemy, TurnRate_LimitsRotationPerFrame, MultipleEnemies_TargetsClosest, TargetWithDeadTag_Retargets, AllEnemiesDead_FliesStraight, RocketWithDeadTag_NotProcessed, AlreadyFacingTarget_DirectionUnchanged, TargetEntityDestroyed_Retargets)
**Why human:** Unity ECS SystemBase, EntityManager, SystemAPI требуют Unity Editor runtime для выполнения тестов

### 2. EditMode тесты системы перезарядки

**Test:** Запустить тесты в RocketAmmoSystemTests (5 тестов)
**Expected:** Все 5 тестов GREEN (Reload_IncrementsCurrentAmmo_ByOne, Reload_DoesNotExceedMaxAmmo, Reload_TimerDecreases_WhenNotFull, Reload_ResetsTimer_AfterReload, Reload_MultipleEntities_IndependentTimers)
**Why human:** Unity ECS ISystem требует Unity Editor runtime

### 3. EntityFactory тесты ракет

**Test:** Запустить CreateRocket_CreatesEntityWithAllComponents и CreateShip_HasRocketAmmoData из EntityFactoryTests
**Expected:** Оба теста GREEN
**Why human:** Unity ECS EntityManager API недоступен вне Editor

### Gaps Summary

Нет блокирующих gaps. Все 5 roadmap success criteria удовлетворены на уровне кода:
- Наведение с turn rate (SC1) -- полная реализация EcsRocketGuidanceSystem
- Переключение цели при DeadTag (SC2) -- явная проверка Exists + HasComponent DeadTag
- Самоуничтожение по LifeTimeData (SC3) -- CreateRocket добавляет LifeTimeData, существующие системы обрабатывают
- Инкрементальная перезарядка (SC4) -- EcsRocketAmmoSystem, уменьшение при запуске отложено до Phase 13
- EditMode тесты (SC5) -- 16 тестов покрывают все компоненты и системы

Единственное отложенное: "боезапас уменьшается при запуске" -- это область Phase 13 (Input & Game Integration), где реализуется запуск ракеты клавишей R.

---

_Verified: 2026-04-05T22:15:00Z_
_Verifier: Claude (gsd-verifier)_
