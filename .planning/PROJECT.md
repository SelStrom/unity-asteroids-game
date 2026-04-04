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

(Определяются при создании следующего milestone через `/gsd:new-milestone`)

### Out of Scope

- Полный DOTS (без GameObjects) — Entities Graphics не поддерживает SpriteRenderer и WebGL
- DOTS Physics 2D — пакет не существует в production-ready виде
- 2D Lighting — новый функционал, не часть миграции 1:1
- Исправление существующих багов — миграция 1:1, баги в отдельном milestone
- Мобильные платформы — будущие планы, текущий scope: Editor + Windows + WebGL
- Новые игровые механики — только миграция существующего функционала

## Context

- **Движок:** Unity 6.3, C# 9.0, Mono backend
- **Render:** URP 2D Renderer с Post-Processing (Bloom, Vignette)
- **Архитектура:** Гибридный DOTS — 8 ECS ISystem (Thrust, Rotate, Move, Gun, Laser, ShootTo, MoveTo, CollisionHandler) + Bridge Layer (GameObjectSyncSystem, ObservableBridgeSystem, DeadEntityCleanupSystem) + GameObjects для рендера + MVVM UI
- **Кодовая база:** ~4700 LOC scripts, ~4350 LOC tests, 142 теста
- **Платформы:** Editor, WebGL, WindowsStandalone64

## Constraints

- **Функциональная эквивалентность:** Геймплей 1:1 после каждого изменения
- **Гибридный DOTS:** Entities для логики, GameObjects для UI/визуала/физики
- **Обратная совместимость shtl-mvvm:** Фикс работает начиная с Unity 2022.3

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

---
*Last updated: 2026-04-04 after v1.1.0 milestone*
