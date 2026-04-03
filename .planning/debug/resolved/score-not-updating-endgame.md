---
status: resolved
trigger: "Score не обновляется в окне результатов (End Game screen)"
created: 2026-04-03T00:00:00Z
updated: 2026-04-03T12:12:00Z
---

## Current Focus

hypothesis: ECS ScoreData не синхронизируется обратно в Model.Score — bridge пропущен
test: Поиск кода синхронизации ScoreData -> Model.Score в ObservableBridgeSystem и всех bridge-файлах
expecting: Отсутствие такого кода подтвердит гипотезу
next_action: Вернуть диагноз

## Symptoms

expected: При уничтожении врагов очки начисляются и отображаются на End Game screen
actual: End Game screen показывает score: 0
errors: нет (ошибок в консоли нет)
reproduction: Уничтожить врагов, погибнуть, посмотреть End Game screen
started: После миграции на ECS (Phase 05)

## Eliminated

(нет — первая гипотеза подтвердилась)

## Evidence

- timestamp: 2026-04-03T00:01:00Z
  checked: EcsCollisionHandlerSystem.cs — начисляет ли ECS очки
  found: Да, EcsCollisionHandlerSystem.AddScore() корректно обновляет ScoreData.Value (строка 134-139)
  implication: Очки начисляются в ECS-мире корректно

- timestamp: 2026-04-03T00:02:00Z
  checked: ObservableBridgeSystem.cs — синхронизирует ли ECS ScoreData обратно в Model.Score
  found: НЕТ. ObservableBridgeSystem синхронизирует HudData (координаты, скорость, вращение, лазер) и ShipViewModel, но НЕ содержит никакого кода для синхронизации ScoreData -> Model.Score
  implication: ScoreData обновляется только в ECS-мире, Model.Score остаётся 0

- timestamp: 2026-04-03T00:03:00Z
  checked: GameScreen.ShowEndGame() — откуда берёт score для отображения
  found: Строка 161: scoreVm.Score.Value = $"score: {_data.Model.Score}" — читает из Model.Score
  implication: Всегда отображает 0, потому что Model.Score никогда не обновляется в ECS-режиме

- timestamp: 2026-04-03T00:04:00Z
  checked: Application.OnDeadEntity() — вызывает ли ReceiveScore при гибели врага в ECS
  found: НЕТ. OnDeadEntity (строки 136-166) обрабатывает визуальные эффекты и дробление астероидов, но НЕ вызывает _model.ReceiveScore()
  implication: Второй потенциальный путь начисления очков тоже не работает в ECS-режиме

## Resolution

root_cause: В ECS-режиме очки начисляются только в ECS ScoreData (через EcsCollisionHandlerSystem), но ObservableBridgeSystem не синхронизирует ScoreData.Value обратно в Model.Score. GameScreen.ShowEndGame() читает Model.Score (строка 161), который всегда равен 0.
fix: (не применяется — только диагностика)
verification: (не применяется)
files_changed: []
