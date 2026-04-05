---
phase: 15-hud
plan: 01
subsystem: hud-rocket
tags: [hud, mvvm, ecs-bridge, rocket]
dependency_graph:
  requires: [RocketAmmoData, HudData, ObservableBridgeSystem]
  provides: [HudData.RocketAmmoCount, HudData.RocketReloadTime, HudData.IsRocketReloadTimeVisible, ObservableBridgeSystem.SetRocketMaxAmmo]
  affects: [GameScreen.ActivateHud]
tech_stack:
  added: []
  patterns: [MVVM reactive binding, ECS-to-ViewModel bridge]
key_files:
  created: []
  modified:
    - Assets/Scripts/View/HudVisual.cs
    - Assets/Scripts/Bridge/ObservableBridgeSystem.cs
    - Assets/Scripts/Application/Screens/GameScreen.cs
    - Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs
decisions: []
metrics:
  duration_seconds: 119
  completed: "2026-04-05T22:45:01Z"
  tasks_completed: 2
  tasks_total: 2
  files_modified: 4
---

# Phase 15 Plan 01: HUD Rocket MVVM Chain Summary

Полная MVVM цепочка ECS -> HUD для ракетного боезапаса и таймера перезарядки по паттерну лазера

## What Was Done

### Task 1: HudData ViewModel + HudVisual View (6bfe000)
- Добавлены 3 ReactiveValue поля в HudData: RocketAmmoCount, RocketReloadTime, IsRocketReloadTimeVisible
- Добавлены 2 SerializeField в HudVisual: _rocketAmmoCount, _rocketReloadTime
- Добавлены 3 Bind в OnConnected() для привязки rocket данных к TMP_Text элементам

### Task 2: ObservableBridgeSystem + GameScreen + тесты (2368b40)
- ObservableBridgeSystem: добавлено поле _rocketMaxAmmo и метод SetRocketMaxAmmo
- ObservableBridgeSystem: расширен Query с RefRO<RocketAmmoData>, запись rocket данных в HudData
- GameScreen.ActivateHud(): добавлен вызов bridge.SetRocketMaxAmmo(_configs.Rocket.MaxAmmo)
- 3 новых теста: PushesRocketData_ToHudData, PushesRocketReloadVisibility_HiddenWhenFull, PushesRocketReloadTime_ToHudData

## Deviations from Plan

None -- план выполнен точно как написан.

## Known Stubs

None -- все данные подключены к реальным ECS компонентам через bridge. SerializeField для TMP_Text ещё не назначены в сцене (это задача Plan 02).

## Self-Check: PASSED

- All 4 modified files exist on disk
- Commit 6bfe000: FOUND
- Commit 2368b40: FOUND
