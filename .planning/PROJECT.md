# Asteroids

## What This Is

Классическая аркадная игра Asteroids на Unity 6.3 с гибридным DOTS. ECS управляет логикой (10 Burst-систем, включая ракетные), GameObjects — рендерингом и физикой, MVVM (shtl-mvvm) — UI. URP 2D Renderer с Post-Processing. Онлайн-лидерборд через Unity Gaming Services. Самонаводящиеся ракеты с trail и VFX.

## Core Value

Играбельная классическая механика Asteroids с онлайн-лидербордом — на современном стеке Unity с ECS-ядром для масштабируемости.

## Requirements

### Validated

- ✓ Фикс shtl-mvvm для совместимости с Unity 6.3 и обратной совместимости с 2022.3+ — v1.1.0
- ✓ Апгрейд на Unity 6.3 с адаптацией к встроенному TMP — v1.1.0
- ✓ Миграция с Built-in Render Pipeline на URP — v1.1.0
- ✓ Переход геймплейной логики на гибридный DOTS (ECS + GameObjects) — v1.1.0
- ✓ Удаление legacy MonoBehaviour-систем, единый ECS data path — v1.1.0
- ✓ TDD-покрытие: 142 теста (135 EditMode + 7 PlayMode) — v1.1.0
- ✓ UFO-Asteroid коллизии, UFO AI wiring — v1.1.0
- ✓ ECS tech debt cleanup (ordering, vestigial fields, dead bindings) — v1.1.0
- ✓ Самонаводящиеся ракеты: запуск по R, наведение на ближайшего врага, ограниченный turn rate — v1.2.0
- ✓ Коллизия ракеты с астероидами и UFO, начисление очков, уничтожение ракеты — v1.2.0
- ✓ Респавн ракет по таймеру, количество и время из ScriptableObject конфига — v1.2.0
- ✓ Визуал ракеты: уменьшенный спрайт корабля + инверсионный след (ParticleSystem) + взрыв VFX — v1.2.0
- ✓ HUD: отображение доступных ракет и таймера перезарядки — v1.2.0
- ✓ ECS-интеграция: RocketGuidanceSystem, RocketAmmoSystem, CollisionHandler ветки — v1.2.0
- ✓ TDD: 19+ юнит-тестов, 5 интеграционных тестов lifecycle, MCP-верификация — v1.2.0

### Active

(Определяются в следующем milestone)

### Out of Scope

- Полный DOTS (без GameObjects) — Entities Graphics не поддерживает SpriteRenderer и WebGL
- DOTS Physics 2D — пакет не существует в production-ready виде
- 2D Lighting — новый функционал, не часть текущего scope
- Мобильные платформы — будущие планы, текущий scope: Editor + Windows + WebGL
- Множественные типы ракет — один тип ракеты для v1.2

## Context

- **Движок:** Unity 6.3, C# 9.0, Mono backend
- **Render:** URP 2D Renderer с Post-Processing (Bloom, Vignette)
- **Архитектура:** Гибридный DOTS — 10 ECS ISystem (Thrust, Rotate, Move, Gun, Laser, ShootTo, MoveTo, CollisionHandler, RocketGuidance, RocketAmmo) + Bridge Layer (GameObjectSyncSystem, ObservableBridgeSystem, DeadEntityCleanupSystem, ShootEventProcessorSystem) + GameObjects для рендера + MVVM UI
- **Кодовая база:** ~6000 LOC scripts, 160+ тестов
- **Платформы:** Editor, WebGL, WindowsStandalone64

## Current Milestone

Определяется через `/gsd-new-milestone`.

## Constraints

- **Гибридный DOTS:** Entities для логики, GameObjects для UI/визуала/физики
- **Обратная совместимость shtl-mvvm:** Фикс работает начиная с Unity 2022.3
- **TDD-парадигма:** Весь новый функционал покрыт тестами, human validation только в исключительных случаях

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Unity 6.3 первым шагом | Апгрейд редактора — базовое требование для URP и DOTS | ✓ Good |
| Гибридный DOTS вместо полного | UI на GameObjects проще поддерживать с shtl-mvvm, Entities Graphics не поддерживает SpriteRenderer | ✓ Good |
| Баги out of scope | Миграция 1:1, исправления в отдельном milestone | ✓ Good |
| 2D rotation via sin/cos | Burst-совместимость вместо Quaternion | ✓ Good |
| ShipPositionData — отдельный singleton | MoveSystem остается Burst-совместимой | ✓ Good |
| DeadTag вместо Kill(model) | Единый путь уничтожения через DeadEntityCleanupSystem | ✓ Good |
| Standalone ActionScheduler | Managed callbacks несовместимы с Burst | ✓ Good |
| EntityType enum вместо TryFindModel | Определение типа entity без зависимости от Model | ✓ Good |
| Simple seek вместо Proportional Navigation | Аркадная простота, достаточно для gameplaу | ✓ Good |
| RocketShootEvent singleton buffer | Единый механизм shoot events для пуль и ракет | ✓ Good |
| ScoreValue на enemy, не на ракете | Очки начисляются по типу врага, не оружия | ✓ Good |
| Trail через ParticleSystem + Editor-скрипт | Программная настройка trail, не ручная в Inspector | ✓ Good |

## Shipped Milestones

- **v1.1.0** — Техническая миграция (2026-04-04): Unity 6.3 + URP + гибридный DOTS. [Details](MILESTONES.md)
- **v1.2.0** — Самонаводящиеся ракеты (2026-04-06): ECS ракеты с наведением, коллизиями, trail VFX и HUD. [Details](MILESTONES.md)

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-06 after v1.2.0 milestone complete*
