# Retrospective

## Milestone: v1.2.0 — Самонаводящиеся ракеты

**Shipped:** 2026-04-06
**Phases:** 8 | **Plans:** 16

### What Was Built
- ECS-компоненты ракеты (RocketTag, RocketTargetData, RocketAmmoData) + системы наведения и перезарядки
- TDD-driven collision ветки с DeadTag и начислением очков
- Bridge ECS->GameObject: RocketVisual, GameObjectSyncSystem ветка для ракет
- Input R -> ECS -> Visual пайплайн с guard на боезапас
- ScriptableObject конфигурация + trail ParticleSystem + VFX взрыв
- HUD: боезапас и таймер перезарядки через ObservableBridgeSystem

### What Worked
- TDD-first подход: 19+ юнит-тестов + 5 интеграционных написаны до/во время реализации
- Декомпозиция на фазы по слоям (ECS core -> collision -> bridge -> input -> config -> HUD) — каждая фаза изолированно тестируема
- ECS-паттерны из v1.1.0 (DeadTag, GameObjectSyncSystem, ObservableBridgeSystem) переиспользованы без модификации
- MCP-инструменты для создания Unity ассетов в планах (префабы, материалы, сцена)

### What Was Inefficient
- Phase 17 (doc cleanup) не исполнена — SUMMARY frontmatter и REQUIREMENTS.md чекбоксы не обновлялись по ходу работы
- Score=0 в GameData.asset обнаружен только аудитом — Editor-скрипт и ручная настройка ассетов требуют дополнительного checkpoint
- ScoreValue на entity ракеты создан, но не используется collision system — orphan data

### Patterns Established
- Gap closure фазы (16, 17) как формальный механизм закрытия audit findings
- Editor-скрипты для программной настройки префабов (RocketPrefabSetup.cs) вместо ручной работы в Inspector
- ShootEvent buffer pattern: единый механизм для bullet и rocket shoot events

### Key Lessons
- SUMMARY frontmatter `requirements-completed` должен заполняться при исполнении, не откладываться на отдельную фазу
- Значения ассетов (ScriptableObject) требуют автоматической верификации через grep/MCP, не только визуальной
- Orphan data (ScoreValue) допустим как forward-looking, но должен документироваться в PLAN.md

### Cost Observations
- Sessions: ~3 (discuss/plan/execute per phase group)
- Notable: Phase 16 (asset fix) исполнена inline без subagent — эффективно для 1-2 задач

## Cross-Milestone Trends

| Metric | v1.1.0 | v1.2.0 |
|--------|--------|--------|
| Phases | 9 | 8 |
| Plans | 27 | 16 |
| Timeline | 2 days | 2 days |
| Test count | 142 | 160+ |
| LOC | ~4700 | ~6000 |
