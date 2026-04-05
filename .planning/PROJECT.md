# Asteroids

## What This Is

Классическая аркадная игра Asteroids на Unity 6.3 с гибридным DOTS. ECS управляет логикой (8 Burst-систем), GameObjects — рендерингом и физикой, MVVM (shtl-mvvm) — UI. URP 2D Renderer с Post-Processing. Онлайн-лидерборд через Unity Gaming Services.

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

### Active

- [ ] Самонаводящиеся ракеты: запуск по кнопке R, полёт по дуге к ближайшей цели
- [ ] Коллизия ракеты с астероидами и UFO (включая случайные столкновения по пути)
- [ ] Респавн ракет по таймеру, количество и время из конфигов
- [ ] Визуал ракеты: уменьшенный спрайт корабля + инверсионный след (частицы)
- [ ] HUD: отображение доступных ракет и таймера респавна
- [ ] Интеграция в ECS-архитектуру (RocketSystem, RocketComponent, etc.)
- [ ] TDD-покрытие: юнит-тесты, интеграционные тесты, MCP-верификация

### Out of Scope

- Полный DOTS (без GameObjects) — Entities Graphics не поддерживает SpriteRenderer и WebGL
- DOTS Physics 2D — пакет не существует в production-ready виде
- 2D Lighting — новый функционал, не часть текущего scope
- Мобильные платформы — будущие планы, текущий scope: Editor + Windows + WebGL
- Множественные типы ракет — один тип ракеты для v1.2

## Context

- **Движок:** Unity 6.3, C# 9.0, Mono backend
- **Render:** URP 2D Renderer с Post-Processing (Bloom, Vignette)
- **Архитектура:** Гибридный DOTS — 8 ECS ISystem (Thrust, Rotate, Move, Gun, Laser, ShootTo, MoveTo, CollisionHandler) + Bridge Layer (GameObjectSyncSystem, ObservableBridgeSystem, DeadEntityCleanupSystem) + GameObjects для рендера + MVVM UI
- **Кодовая база:** ~4700 LOC scripts, ~4350 LOC tests, 142 теста
- **Платформы:** Editor, WebGL, WindowsStandalone64

## Current Milestone: v1.2.0 Самонаводящиеся ракеты

**Goal:** Добавить систему самонаводящихся ракет с полным TDD-покрытием, вписанную в ECS + визуал архитектуру.

**Target features:**
- Запуск ракеты по кнопке R, полёт по дуге к ближайшей цели (астероид/UFO)
- Коллизия ракеты с любым врагом по пути
- Респавн ракет по таймеру, количество и время из конфигов
- Визуал: уменьшенный спрайт корабля + инверсионный след (частицы)
- HUD: доступные ракеты и таймер респавна
- Полная ECS-интеграция по аналогии с Bullet/Laser
- TDD: юнит, интеграционные тесты, MCP-верификация

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

## Shipped Milestones

- **v1.1.0** — Техническая миграция (2026-04-04): Unity 6.3 + URP + гибридный DOTS. [Details](MILESTONES.md)

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
*Last updated: 2026-04-05 after v1.2.0 milestone start*
