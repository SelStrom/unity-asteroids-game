---
status: passed
phase: 05-bridge-layer-integration
source: [05-VERIFICATION.md]
started: 2026-04-03T12:00:00Z
updated: 2026-04-03T12:20:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Full gameplay cycle
expected: Launch game in Editor, verify ship controls (WASD), enemy spawning, bullet/laser shooting, asteroid splitting, UFO AI, collisions cause game over, score updates, leaderboard
result: issue
reported: "1. При использовании лазера, большой астеройд не уничтожается, маленькие появляются в неправильных координатах. 2. Если при использовании лазер игрока убили, эффект лазера остается на сцене и не уничтожается. 3. Score в окне результатов не обновляется, не понятно, толи это проблема UI, толи в модели не начисляется."
severity: major

### 2. HUD data via ObservableBridgeSystem
expected: Real-time UI updates — coordinates, speed, rotation angle, laser charge count, laser reload timer all update correctly during gameplay
result: pass

### 3. UFO collisions
expected: UFO destruction works via MonoBehaviour collision path (UfoVisual OnCollisionEnter2D), score increments on UFO kill
result: pass
note: "UFO уничтожается корректно, взрыв отображается. Начисление очков не проверено — зависит от бага score (gap 3 из теста 1)"

## Summary

total: 3
passed: 2
issues: 1
pending: 0
skipped: 0
blocked: 0

## Gaps

- truth: "Лазер уничтожает большой астероид, осколки появляются на позиции родителя"
  status: resolved
  reason: "User reported: При использовании лазера, большой астеройд не уничтожается, маленькие появляются в неправильных координатах."
  severity: major
  test: 1
  root_cause: "ProcessShootEvents() вызывает Kill(model) для лазерных попаданий, но Kill() ставит _killed=true на C#-модели, а Model.Update() в ECS-режиме не вызывается — очистка мёртвых сущностей не срабатывает. ECS-entity не получает DeadTag. Позиции осколков читаются из MoveComponent.Position, которая заморожена на значении при спавне (ECS-позиция в MoveData.Position не синхронизируется обратно)."
  artifacts:
    - path: "Assets/Scripts/Application/Game.cs"
      issue: "ProcessShootEvents (строки 196-226) вызывает Kill(model) вместо добавления DeadTag к ECS-entity"
    - path: "Assets/Scripts/Application/Game.cs"
      issue: "Kill() (строки 316-342) читает устаревшую model.Move.Position.Value"
    - path: "Assets/Scripts/Application/Application.cs"
      issue: "OnUpdate (строка 180) в ECS-режиме не вызывает _model.Update() — killed entities не очищаются"
  missing:
    - "Лазерный путь должен помечать ECS-entity через DeadTag вместо Kill(model)"
    - "Использовать go.transform.position для позиции осколков (как в OnDeadEntity)"
  debug_session: ".planning/debug/laser-asteroid-kill-bug.md"

- truth: "Эффект лазера уничтожается при гибели игрока"
  status: resolved
  reason: "User reported: Если при использовании лазер игрока убили, эффект лазера остается на сцене и не уничтожается."
  severity: major
  test: 1
  root_cause: "Stop() вызывает ActionScheduler.ResetSchedule(), который безусловно очищает ВСЕ pending-действия, включая запланированный Release для лазерного LineRenderer. VFX не является entity, не отслеживается отдельно — теряется без ссылок."
  artifacts:
    - path: "Assets/Scripts/Application/Game.cs"
      issue: "Stop() (строка 78) вызывает ResetSchedule(), убивая pending cleanup лазерного VFX"
    - path: "Assets/Scripts/Application/Game.cs"
      issue: "Лазерный VFX создаётся (строки 196-227, 356-364) с отложенным Release через ActionScheduler"
    - path: "Assets/Scripts/Model/ActionScheduler.cs"
      issue: "ResetSchedule() (строки 62-66) безусловно очищает все действия"
  missing:
    - "Перед ResetSchedule() принудительно Release-ить активные лазерные VFX"
    - "Или вести отдельный список активных лазерных LineRenderer-объектов"
  debug_session: ".planning/debug/laser-vfx-persist-on-death.md"

- truth: "Score обновляется в окне результатов при уничтожении врагов"
  status: resolved
  reason: "User reported: Score в окне результатов не обновляется, не понятно, толи это проблема UI, толи в модели не начисляется."
  severity: major
  test: 1
  root_cause: "В ECS-режиме очки начисляются в ScoreData (EcsCollisionHandlerSystem.AddScore), но ObservableBridgeSystem не синхронизирует ScoreData в Model.Score. GameScreen.ShowEndGame() читает Model.Score, который всегда 0."
  artifacts:
    - path: "Assets/Scripts/Bridge/ObservableBridgeSystem.cs"
      issue: "Отсутствует синхронизация ScoreData -> Model.Score"
    - path: "Assets/Scripts/Application/Screens/GameScreen.cs"
      issue: "ShowEndGame() (строка 161) читает Model.Score, который в ECS-режиме всегда 0"
    - path: "Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs"
      issue: "AddScore (строки 134-139) — единственное место начисления очков в ECS"
  missing:
    - "Добавить в ObservableBridgeSystem синхронизацию ScoreData -> Model.Score"
  debug_session: ".planning/debug/score-not-updating-endgame.md"
