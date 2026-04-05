# Phase 13: Input & Game Integration - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md -- this log preserves the alternatives considered.

**Date:** 2026-04-05
**Phase:** 13-Input & Game Integration
**Mode:** auto (discuss workflow)
**Areas discussed:** Input System, Механизм запуска ракеты, Сброс при рестарте, Тестирование

---

## Input System: кнопка R

| Option | Description | Selected |
|--------|-------------|----------|
| Новый action Rocket (Button) + OnRocketAction event | По паттерну Attack/Laser -- единообразно с существующим кодом | ✓ |
| Shared action с Attack (combo key) | Сложнее, ломает единообразие | |

**User's choice:** [auto] Новый action Rocket (Button) + OnRocketAction event (recommended default)
**Notes:** Полная аналогия с OnAttack/OnLaser. Кнопка R не конфликтует с существующими биндингами.

---

## Механизм запуска ракеты (ECS pipeline)

| Option | Description | Selected |
|--------|-------------|----------|
| Shooting flag в RocketAmmoData | По паттерну GunData/LaserData -- Game устанавливает flag, ECS system обрабатывает | ✓ |
| Прямая генерация RocketShootEvent в Game | Ломает ECS encapsulation -- Game не должен писать в DynamicBuffer | |
| Отдельный RocketShootData компонент | Избыточно -- RocketAmmoData уже на ship entity | |

**User's choice:** [auto] Shooting flag в RocketAmmoData (recommended default)
**Notes:** Единообразный pipeline: Game -> flag -> EcsRocketAmmoSystem -> RocketShootEvent -> ShootEventProcessorSystem -> CreateRocket.

---

## Сброс при рестарте

| Option | Description | Selected |
|--------|-------------|----------|
| ReleaseAllGameEntities + ClearEcsEventBuffers | Существующий Restart() уже уничтожает все entity; нужна только очистка RocketShootEvent буфера | ✓ |
| Отдельный ResetRocketAmmo() | Избыточно -- ship entity уничтожается и пересоздаётся с полным боезапасом | |

**User's choice:** [auto] ReleaseAllGameEntities + ClearEcsEventBuffers (recommended default)
**Notes:** Ship entity полностью пересоздаётся в Start() -> CreateShip() с rocketMaxAmmo: 3.

---

## Тестирование

| Option | Description | Selected |
|--------|-------------|----------|
| EditMode тесты на EcsRocketAmmoSystem shooting logic | Тесты декремента амmo, генерации event, блокировки при пустом боезапасе | ✓ |
| E2E тесты с моком Input System | Сложность мока InputActions непропорциональна ценности | |

**User's choice:** [auto] EditMode тесты на EcsRocketAmmoSystem shooting logic (recommended default)
**Notes:** Входная точка (Game.OnRocket -> RocketAmmoData.Shooting) тривиальна и покрывается ручной проверкой. Ценные тесты -- на ECS-логике.

---

## Claude's Discretion

- Guard `CurrentAmmo > 0` -- где именно (Game или ECS)
- Порядок action в inputactions файле

## Deferred Ideas

None -- analysis stayed within phase scope
