---
status: resolved
trigger: "Лазер не уничтожает большой астероид. Маленькие осколки появляются в неправильных координатах."
created: 2026-04-03T00:00:00Z
updated: 2026-04-03T12:12:00Z
---

## Current Focus

hypothesis: В ECS-режиме Game.ProcessShootEvents() обрабатывает лазерные попадания через Game.Kill(), который: (1) не помечает ECS-entity DeadTag, (2) читает устаревшую позицию из MonoBehaviour-модели вместо Transform/ECS.
test: Проверены оба кодовых пути (collision vs laser), сравнены механизмы уничтожения.
expecting: Collision path корректно использует DeadTag + DeadEntityCleanupSystem. Laser path этого не делает.
next_action: Возврат диагностики.

## Symptoms

expected: Лазер уничтожает большой астероид, осколки появляются на его позиции
actual: Большой астероид не уничтожается. Осколки появляются на начальной позиции спавна астероида.
errors: Нет ошибок в консоли
reproduction: В ECS-режиме (_useEcs=true) выстрелить лазером по большому астероиду
started: После интеграции ECS bridge layer (Phase 05)

## Eliminated

(нет)

## Evidence

- timestamp: 2026-04-03
  checked: Application.cs OnUpdate (строки 178-190)
  found: В ECS-режиме _model.Update() НЕ вызывается — только ActionScheduler.Update() и ProcessShootEvents()
  implication: MonoBehaviour MoveSystem не обновляет model.Move.Position для астероидов. Эта позиция "замораживается" на значении при создании.

- timestamp: 2026-04-03
  checked: Game.ProcessShootEvents() — лазерный путь (строки 182-227)
  found: Raycast находит астероид (по GameObject/Transform), затем вызывает Kill(model) на MonoBehaviour-модели
  implication: Kill() вызывает model.Kill() (ставит _killed=true) и читает model.Move.Position.Value для спавна осколков

- timestamp: 2026-04-03
  checked: Game.Kill() (строки 316-342)
  found: Kill() не добавляет DeadTag к ECS-entity. Не вызывает _catalog.Release(). Только ставит _killed=true на C# модели.
  implication: ECS-entity продолжает жить и обновляться. GameObject не удаляется.

- timestamp: 2026-04-03
  checked: Model.Update() (строки 144-153)
  found: Очистка мёртвых сущностей происходит ТОЛЬКО в Model.Update(), который не вызывается в ECS-режиме
  implication: OnEntityDestroyed никогда не срабатывает → _catalog.Release() не вызывается → ни ECS-entity, ни GameObject не уничтожаются

- timestamp: 2026-04-03
  checked: Application.OnDeadEntity() (строки 136-165)
  found: Корректный ECS-путь уничтожения: использует go.transform.position (актуальная позиция), создаёт осколки, вызывает ReleaseByGameObject
  implication: Этот путь работает для collision-based kills (через DeadTag + DeadEntityCleanupSystem), но лазерный путь его обходит

- timestamp: 2026-04-03
  checked: AsteroidModel.Move.Position.Value vs go.transform.position
  found: В ECS-режиме GameObjectSyncSystem обновляет Transform из ECS MoveData. Но model.Move.Position (ObservableValue) не обновляется
  implication: Kill() читает model.Move.Position.Value — это значение при создании, а не текущая позиция

## Resolution

root_cause: |
  Два бага с одной первопричиной — в ECS-режиме Game.ProcessShootEvents() обрабатывает лазерные попадания через Game.Kill(model),
  который был написан для MonoBehaviour-режима и не адаптирован под ECS:

  1. **Астероид не уничтожается:** Kill() вызывает model.Kill() (ставит _killed=true), но в ECS-режиме Model.Update() не запускается,
     поэтому мёртвая сущность никогда не очищается. ECS-entity не получает DeadTag, GameObject не удаляется.

  2. **Осколки в неправильных координатах:** Kill() читает asteroidModel.Move.Position.Value для позиции осколков.
     Но в ECS-режиме MonoBehaviour MoveSystem не обновляет эту позицию — она остаётся на начальном значении при спавне.
     Актуальная позиция находится в ECS MoveData.Position (синхронизируется в Transform через GameObjectSyncSystem).

fix: (не применялся — режим find_root_cause_only)
verification: (не выполнялась)
files_changed: []
