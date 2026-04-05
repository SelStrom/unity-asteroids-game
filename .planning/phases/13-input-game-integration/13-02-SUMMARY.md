---
phase: 13-input-game-integration
plan: 02
subsystem: game-integration
tags: [input, ecs, rocket, game-loop]
dependency_graph:
  requires: [13-01]
  provides: [rocket-input-to-ecs-pipeline]
  affects: [Game.cs]
tech_stack:
  added: []
  patterns: [input-handler-with-ammo-guard, ecs-event-buffer-clear]
key_files:
  created: []
  modified:
    - Assets/Scripts/Application/Game.cs
decisions:
  - "CurrentAmmo <= 0 guard в OnRocket() предотвращает лишний SetComponentData при зажатой R с пустым боезапасом"
metrics:
  duration: "48s"
  completed: "2026-04-05"
  tasks_completed: 1
  tasks_total: 1
  files_modified: 1
---

# Phase 13 Plan 02: Game.OnRocket Handler Integration Summary

Game.OnRocket() handler с guard на пустой боезапас, подписки Start/Stop, очистка RocketShootEvent буфера при рестарте

## Changes Made

### Task 1: Game.OnRocket handler + Start/Stop subscriptions + ClearEcsEventBuffers

**Commit:** c11ab0d

В `Game.cs` внесены три изменения:

1. **OnRocket() handler** (строка 291) -- по аналогии с OnAttack/OnLaser, но с дополнительным guard `CurrentAmmo <= 0`. Читает RocketAmmoData, устанавливает Shooting=true, Direction и ShootPosition из RotateData/MoveData корабля.

2. **Start/Stop subscriptions** -- `_playerInput.OnRocketAction += OnRocket` в Start() (строка 48), `_playerInput.OnRocketAction -= OnRocket` в Stop() (строка 67). Паттерн идентичен OnAttack/OnLaser.

3. **ClearEcsEventBuffers** -- блок очистки RocketShootEvent буфера (строки 129-139) между LaserShootEvent и CollisionEventData. Паттерн идентичен существующим блокам очистки.

## Deviations from Plan

None -- план выполнен точно как написан.

## Verification Results

Все acceptance criteria подтверждены grep-проверками:
- `private void OnRocket()` -- присутствует
- `GetComponentData<RocketAmmoData>(entity)` -- присутствует
- `rocketAmmo.CurrentAmmo <= 0` guard -- присутствует
- `rocketAmmo.Shooting = true` -- присутствует
- `OnRocketAction += OnRocket` в Start() -- присутствует
- `OnRocketAction -= OnRocket` в Stop() -- присутствует
- `CreateEntityQuery(typeof(RocketShootEvent))` в ClearEcsEventBuffers -- присутствует
- `GetBuffer<RocketShootEvent>` с `.Clear()` -- присутствует

## Self-Check: PASSED

- Game.cs: FOUND
- Commit c11ab0d: FOUND
- SUMMARY.md: FOUND
