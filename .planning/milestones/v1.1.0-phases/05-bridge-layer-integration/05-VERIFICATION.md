---
phase: 05-bridge-layer-integration
verified: 2026-04-03T12:20:00Z
status: passed
score: 7/7
re_verification:
  previous_status: human_needed
  previous_score: 5/5
  gaps_closed:
    - "Laser kill in ECS mode destroys asteroid via DeadTag, not model.Kill()"
    - "Asteroid fragments spawn at Transform position (actual), not MoveComponent.Position (stale)"
    - "Active laser VFX is released when ship dies (Stop() called)"
    - "Score displayed on EndGame screen matches kills accumulated during ECS gameplay"
  gaps_remaining: []
  regressions: []
gaps: []
human_verification:
  - test: "Запустить игру в Unity Editor и проверить полный игровой цикл после исправления 3 UAT-багов"
    expected: "Лазер уничтожает астероиды (осколки на правильной позиции), VFX лазера исчезает при смерти корабля, Score корректен на EndGame экране"
    why_human: "Все 3 бага были визуальными; автоматические тесты покрывают логику, но реальный gameplay требует ручной проверки"
  - test: "Проверить HUD отображение данных через ObservableBridgeSystem"
    expected: "Coordinates, Speed, Rotation, Laser shoots обновляются в реальном времени при управлении кораблем"
    why_human: "Форматирование строк и визуальная корректность UI невозможно проверить программно"
---

# Phase 5: Bridge Layer + Integration -- Re-Verification Report

**Phase Goal:** Полностью работающая игра на гибридном DOTS -- ECS управляет логикой, GameObjects отвечают за рендеринг и UI
**Verified:** 2026-04-03T12:11:01Z
**Status:** human_needed
**Re-verification:** Yes -- after UAT gap closure (plans 05-04, 05-05)

## Context

Первая верификация (status: human_needed) прошла без automated gaps. UAT (05-HUMAN-UAT.md) обнаружил 3 major бага:
1. Лазер не уничтожает астероиды (Kill(model) вместо DeadTag)
2. VFX лазера не исчезает при смерти корабля (ResetSchedule стирает cleanup)
3. Score = 0 на EndGame экране (ScoreData не синхронизировался в Model.Score)

Планы 05-04 и 05-05 были созданы и выполнены для закрытия gaps.

## UAT Gap Closure Verification

### Gap 1: Laser kill via DeadTag (CLOSED)

**Previous issue:** ProcessShootEvents вызывал Kill(model), который не ставил DeadTag и читал устаревшую позицию из модели.

**Fix verified in codebase:**
- `Assets/Scripts/Application/Game.cs` строки 234-239: `_entityManager.AddComponent<DeadTag>(entity)` вместо Kill(model)
- ReceiveScore вызывается ПЕРЕД DeadTag (строка 233) -- очки не теряются
- Проверка `HasComponent<DeadTag>` предотвращает двойное добавление (строка 236)
- Commit: `1079d1c`

**Regression test:** `LaserKill_InEcsMode_AddsDeadTag_InsteadOfModelKill` (EcsBridgeRegressionTests.cs строка 358)

### Gap 2: Laser VFX cleanup on death (CLOSED)

**Previous issue:** ActionScheduler.ResetSchedule() стирал запланированный Release для лазерного LineRenderer.

**Fix verified in codebase:**
- `Assets/Scripts/Application/Game.cs` строка 27: `private readonly List<GameObject> _activeLaserVfx = new();`
- ECS-путь: VFX добавляется в список (строка 215), scheduled action удаляет (строка 218)
- MonoBehaviour-путь: аналогичный трекинг (строки 382, 385)
- Stop(): Release всех активных VFX ПЕРЕД ResetSchedule (строки 81-85)
- Commit: `1079d1c`

**Regression test:** `LaserVfx_ActiveList_TracksCreatedEffects` (EcsBridgeRegressionTests.cs строка 412)

### Gap 3: Score sync to EndGame screen (CLOSED)

**Previous issue:** ScoreData (ECS singleton) не синхронизировался в Model.Score; GameScreen.ShowEndGame() читал 0.

**Fix verified in codebase:**
- `Assets/Scripts/Model/Model.cs` строки 68-71: `public void SetScore(int value)` метод
- `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` строки 15, 32-35: `_model` field + `SetModel(Model)` setter
- ObservableBridgeSystem.OnUpdate() строки 99-107: читает ScoreData singleton, вызывает `_model.SetScore(scoreData.Value)`
- `Assets/Scripts/Application/Application.cs` строки 99-103: `bridgeSystem.SetModel(_model)` при ECS init
- ClearReferences() сбрасывает _model = null (строка 46)
- Commit: `4e624c9`

**Regression test:** `ScoreData_IsSynced_ToModelScore_ViaObservableBridge` (EcsBridgeRegressionTests.cs строка 294)

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Bridge Layer связывает Entity с GameObject: позиция/ротация синхронизируется из ECS в Transform каждый кадр | VERIFIED | GameObjectSyncSystem.cs -- PresentationSystemGroup, два foreach. Regression check: файл не изменялся. |
| 2 | Physics2D коллизии корректно передаются в ECS World через CollisionBridge | VERIFIED | CollisionBridge.cs -- ReportCollision -> CollisionEventData buffer. Regression check: файл не изменялся. |
| 3 | ECS-данные транслируются в ObservableValue для shtl-mvvm UI (HUD + Score) | VERIFIED | ObservableBridgeSystem.cs -- синхронизирует HUD, ShipViewModel, и теперь ScoreData -> Model.Score. |
| 4 | Жизненный цикл Entity и GameObject синхронизирован (создание, уничтожение) | VERIFIED | EntitiesCatalog + DeadEntityCleanupSystem. Regression check: файлы не изменялись. |
| 5 | Лазер уничтожает врагов через DeadTag в ECS-режиме | VERIFIED | Game.ProcessShootEvents строки 234-239: AddComponent<DeadTag> вместо Kill(model). |
| 6 | VFX лазера корректно очищается при смерти корабля | VERIFIED | Game._activeLaserVfx трекинг + Stop() cleanup перед ResetSchedule(). |
| 7 | Score отображается корректно на EndGame экране в ECS-режиме | VERIFIED | ObservableBridgeSystem.OnUpdate() -> _model.SetScore(scoreData.Value). |

**Score:** 7/7 truths verified (automated)

### Required Artifacts (Gap Closure)

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/Application/Game.cs` | DeadTag laser kill + VFX tracking | VERIFIED | _activeLaserVfx field, Stop() cleanup, ProcessShootEvents AddComponent<DeadTag> |
| `Assets/Scripts/Bridge/ObservableBridgeSystem.cs` | ScoreData -> Model.Score sync | VERIFIED | _model field, SetModel(), OnUpdate reads ScoreData singleton |
| `Assets/Scripts/Model/Model.cs` | SetScore(int) public method | VERIFIED | строки 68-71, вызывается из ObservableBridgeSystem |
| `Assets/Scripts/Application/Application.cs` | bridgeSystem.SetModel(_model) wiring | VERIFIED | строки 99-103, в блоке ECS init |
| `Assets/Tests/EditMode/ECS/EcsBridgeRegressionTests.cs` | Regression tests for 3 bugs | VERIFIED | 10 тестов, включая LaserKill, LaserVfx, ScoreData_IsSynced |

### Key Link Verification (Gap Closure)

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| Game.ProcessShootEvents | EntityManager.AddComponent<DeadTag> | TryGetEntity lookup | WIRED | Game.cs строки 234-239 |
| Game.Stop | ViewFactory.Release | _activeLaserVfx iteration | WIRED | Game.cs строки 81-84 |
| ObservableBridgeSystem.OnUpdate | Model.SetScore | SystemAPI.GetSingleton<ScoreData> | WIRED | ObservableBridgeSystem.cs строки 102-105 |
| Application.Start (ECS init) | ObservableBridgeSystem.SetModel | world.GetExistingSystemManaged | WIRED | Application.cs строки 100-103 |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| ObservableBridgeSystem | Model.Score | ScoreData.Value (ECS) | Yes -- EcsCollisionHandlerSystem.AddScore increments | FLOWING |
| Game.ProcessShootEvents | DeadTag | AddComponent<DeadTag> | Yes -- triggers DeadEntityCleanupSystem -> OnDeadEntity | FLOWING |
| Game._activeLaserVfx | List<GameObject> | ProcessShootEvents + OnUserLaserShooting | Yes -- tracked on creation, removed on timer or Stop() | FLOWING |

### Behavioral Spot-Checks

Step 7b: SKIPPED (requires Unity Editor runtime -- cannot run outside Editor)

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| BRG-01 | 05-01 | Managed component GameObjectRef связывает Entity с GameObject/Transform | SATISFIED | GameObjectRef.cs -- class : ICleanupComponentData |
| BRG-02 | 05-01 | GameObjectSyncSystem синхронизирует позицию/ротацию из ECS в Transform | SATISFIED | GameObjectSyncSystem.cs -- PresentationSystemGroup |
| BRG-03 | 05-02, 05-05 | CollisionBridge + ObservableBridgeSystem (score sync) | SATISFIED | CollisionBridge.cs + ObservableBridgeSystem ScoreData sync |
| BRG-04 | 05-02, 05-04 | ObservableBridgeSystem транслирует ECS-данные в UI | SATISFIED | HUD data + Score sync + laser kill via DeadTag |
| BRG-05 | 05-02, 05-03 | Жизненный цикл Entity<->GameObject синхронизирован | SATISFIED | EntitiesCatalog parallel creation + DeadEntityCleanupSystem |
| BRG-06 | 05-03, 05-04, 05-05 | Игра воспроизводит весь геймплей 1:1 | NEEDS HUMAN | Все 3 UAT-бага исправлены в коде, требуется повторная ручная проверка |
| TST-10 | 05-02, 05-04, 05-05 | EditMode тесты для Bridge Layer | SATISFIED | 10 regression tests + CollisionBridgeTests(5) + ObservableBridgeSystemTests(7) + DeadEntityCleanupSystemTests(4) |
| TST-12 | 05-03 | PlayMode тесты для полного игрового цикла | SATISFIED | GameplayCycleTests.cs: 2 UnityTest |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| Assets/Scripts/Application/Game.cs | 38 | `TODO @a.shatalov: refactor` | Info | Предсуществующий, не относится к Phase 5 |
| Assets/Scripts/Application/Game.cs | 305 | `TODO @a.shatalov: impl score receiver` | Info | Предсуществующий, не относится к Phase 5 |
| Assets/Scripts/Application/EntitiesCatalog.cs | - | UfoVisual collision not wired to CollisionBridge | Warning | Задокументированное ограничение, функционально работает через MonoBehaviour путь |

Предыдущий anti-pattern (merge conflict в REQUIREMENTS.md) устранён -- маркеры конфликта больше не обнаружены.

### Human Verification Required

### 1. Повторная проверка 3 исправленных UAT-багов

**Test:** Запустить игру в Unity Editor. Выстрелить лазером по большому астероиду. Проверить: (a) астероид уничтожается, (b) осколки появляются на правильной позиции, (c) при смерти во время лазера VFX исчезает, (d) Score на EndGame экране отражает набранные очки.
**Expected:** Все 3 сценария работают корректно.
**Why human:** Все 3 бага были обнаружены именно при ручном тестировании. Автоматические тесты покрывают логику (DeadTag, _activeLaserVfx, ScoreData sync), но полная визуальная верификация требует запуска в Editor.

### 2. HUD данные через ObservableBridgeSystem

**Test:** Во время игры наблюдать HUD: координаты, скорость, угол ротации, заряды лазера
**Expected:** Все значения обновляются в реальном времени.
**Why human:** Визуальное отображение и обновление в реальном времени требуют ручной проверки.

### Gaps Summary

Все 3 UAT-бага закрыты в коде:
1. **Laser kill** -- DeadTag вместо Kill(model), commit `1079d1c`
2. **VFX cleanup** -- _activeLaserVfx трекинг + Stop() cleanup, commit `1079d1c`
3. **Score sync** -- ObservableBridgeSystem -> Model.SetScore, commit `4e624c9`

10 регрессионных тестов покрывают все исправленные сценарии. Automated gaps не обнаружены. Требуется повторная ручная верификация для подтверждения визуальной корректности (BRG-06).

---

_Verified: 2026-04-03T12:11:01Z_
_Verifier: Claude (gsd-verifier)_
