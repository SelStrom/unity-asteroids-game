---
phase: 10-ecs-core
plan: 03
subsystem: rocket-ammo-reload
tags: [ecs, rockets, ammo, reload, tdd]
dependency_graph:
  requires: [10-01]
  provides: [EcsRocketAmmoSystem]
  affects: [phase-13-rocket-launch]
tech_stack:
  added: []
  patterns: [incremental-reload, ISystem-struct]
key_files:
  created:
    - Assets/Scripts/ECS/Systems/EcsRocketAmmoSystem.cs
    - Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs
  modified: []
decisions:
  - id: D-14
    summary: "EcsRocketAmmoSystem только перезарядка, без стрельбы (запуск ракеты -- Phase 13)"
metrics:
  duration_seconds: 73
  completed: "2026-04-05T19:39:44Z"
  tasks_completed: 2
  tasks_total: 2
  files_created: 2
  files_modified: 0
---

# Phase 10 Plan 03: EcsRocketAmmoSystem Summary

Система инкрементальной перезарядки боезапаса ракет по паттерну EcsLaserSystem через TDD (RED-GREEN).

## One-liner

ISystem struct перезарядки ракет: +1 CurrentAmmo за ReloadDurationSec, ограничение MaxAmmo, Burst-совместима.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | RED -- failing-тесты перезарядки | 4af4c10 | RocketAmmoSystemTests.cs, EcsRocketAmmoSystem.cs (заглушка) |
| 2 | GREEN -- реализация EcsRocketAmmoSystem | 1a3979c | EcsRocketAmmoSystem.cs |

## Implementation Details

### EcsRocketAmmoSystem

- `[UpdateAfter(typeof(EcsLaserSystem))]` -- выполняется после лазерной системы
- `public partial struct EcsRocketAmmoSystem : ISystem` -- Burst-совместимая ISystem
- Итерация по `RefRW<RocketAmmoData>` без дополнительных тегов
- Логика: если `CurrentAmmo < MaxAmmo`, уменьшает `ReloadRemaining` на `deltaTime`; при `ReloadRemaining <= 0` -- `CurrentAmmo += 1`, таймер сбрасывается к `ReloadDurationSec`
- Без стрельбы и событий -- запуск ракеты будет реализован в Phase 13

### Тесты (5 штук)

1. **Reload_IncrementsCurrentAmmo_ByOne** -- при истечении таймера боезапас растет на 1
2. **Reload_DoesNotExceedMaxAmmo** -- при полном боезапасе ничего не меняется
3. **Reload_TimerDecreases_WhenNotFull** -- частичный тик уменьшает таймер без инкремента
4. **Reload_ResetsTimer_AfterReload** -- таймер сбрасывается к ReloadDurationSec после восстановления
5. **Reload_MultipleEntities_IndependentTimers** -- два entity перезаряжаются независимо

## Deviations from Plan

None -- план выполнен точно как написан.

## Known Stubs

None -- система полностью реализована.

## Requirements Satisfied

- **ROCK-06**: Инкрементальная перезарядка боезапаса ракет
- **TEST-01**: Юнит-тесты для системы перезарядки (5 тестов)

## Self-Check: PASSED

- [x] EcsRocketAmmoSystem.cs exists
- [x] RocketAmmoSystemTests.cs exists
- [x] 10-03-SUMMARY.md exists
- [x] Commit 4af4c10 verified
- [x] Commit 1a3979c verified
