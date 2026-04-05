---
phase: 13-input-game-integration
plan: 01
subsystem: input-ecs-pipeline
tags: [input-system, ecs, rocket, shooting]
dependency_graph:
  requires: []
  provides: [rocket-input-action, rocket-ammo-shooting, rocket-shoot-event]
  affects: [13-02]
tech_stack:
  added: []
  patterns: [EcsGunSystem-pattern-for-shooting, InputAction-event-pattern]
key_files:
  created: []
  modified:
    - Assets/Input/player_actions.inputactions
    - Assets/Scripts/Input/PlayerActions.cs
    - Assets/Scripts/Input/PlayerInput.cs
    - Assets/Scripts/ECS/Components/RocketAmmoData.cs
    - Assets/Scripts/ECS/Systems/EcsRocketAmmoSystem.cs
    - Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs
decisions:
  - Singleton RocketShootEvent buffer создается в SetUp тестов для RequireForUpdate совместимости
metrics:
  duration: 3m
  completed: "2026-04-05T21:05:47Z"
  tasks_completed: 2
  tasks_total: 2
  files_modified: 6
---

# Phase 13 Plan 01: Input Action Rocket + ECS Shooting Pipeline Summary

Input action Rocket с биндингом R подключен к Unity Input System, EcsRocketAmmoSystem генерирует RocketShootEvent при Shooting+ammo>0 с безусловным сбросом флага

## Tasks Completed

### Task 1: Input action Rocket + PlayerInput event
- **Commit:** 1e35bd5
- Добавлен action Rocket (type: Button) в player_actions.inputactions с биндингом `<Keyboard>/r`
- PlayerActions.cs обновлен: поле, свойство, FindAction, callbacks в AddCallbacks/UnregisterCallbacks, метод в IPlayerControlsActions
- PlayerInput.cs: добавлен `public event Action OnRocketAction` и обработчик OnRocket

### Task 2: RocketAmmoData + EcsRocketAmmoSystem shooting logic (TDD)
- **Commit (RED):** dcb11f9 -- 5 failing тестов стрельбы
- **Commit (GREEN):** e84c8a7 -- реализация стрельбы
- RocketAmmoData расширен полями: Shooting, Direction, ShootPosition (с using Unity.Mathematics)
- EcsRocketAmmoSystem: RequireForUpdate<RocketShootEvent>, WithEntityAccess, генерация RocketShootEvent, безусловный сброс Shooting=false
- 5 новых тестов: Shoot_WithAmmo_CreatesRocketShootEvent, Shoot_WithoutAmmo_NoEvent, Shoot_ResetsShootingFlag_Unconditionally, Shoot_WithAmmo_ResetsShootingFlag, Reload_StillWorks_WithShootingFields
- Существующие 5 тестов перезарядки обновлены для работы с RocketShootEvent singleton

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Существующие тесты перезарядки ломались без RocketShootEvent singleton**
- **Found during:** Task 2
- **Issue:** После добавления `state.RequireForUpdate<RocketShootEvent>()` система не запускалась без singleton buffer, что ломало все существующие тесты перезарядки
- **Fix:** Перенесен CreateRocketEventSingleton в SetUp, чтобы все тесты (и старые, и новые) имели singleton buffer
- **Files modified:** Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs
- **Commit:** e84c8a7

## Verification Results

- player_actions.inputactions содержит action "Rocket" с биндингом "<Keyboard>/r"
- PlayerInput.cs содержит event OnRocketAction и подписку на Rocket.performed
- RocketAmmoData содержит Shooting, Direction, ShootPosition
- EcsRocketAmmoSystem генерирует RocketShootEvent и безусловно сбрасывает Shooting
- Все тесты перезарядки совместимы с новым singleton requirement
- 5 новых тестов стрельбы написаны по TDD

## Self-Check: PASSED

All 6 files found, all 3 commits verified.
