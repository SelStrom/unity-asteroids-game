# Phase 10: ECS Core -- данные и логика ракет - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md -- this log preserves the alternatives considered.

**Date:** 2026-04-05
**Phase:** 10-ECS Core
**Mode:** auto
**Areas discussed:** Алгоритм наведения, Выбор цели, ECS-компоненты ракеты, Боезапас и перезарядка

---

## Алгоритм наведения

| Option | Description | Selected |
|--------|-------------|----------|
| Простой seek с turn rate | Поворот Direction к цели на фиксированную дельту каждый кадр -- дугообразная траектория | ✓ |
| Proportional Navigation | Предиктивное наведение с учётом скорости цели | |
| Прямой полёт без наведения | Стреляет в направлении корабля, без корректировки курса | |

**User's choice:** [auto] Простой seek с turn rate (recommended default)
**Notes:** PN исключён в REQUIREMENTS.md. Простой seek с ограниченным turn rate соответствует ROCK-02 и создаёт визуально интересные дуги.

---

## Выбор цели

| Option | Description | Selected |
|--------|-------------|----------|
| Евклидово расстояние без wrap | Ближайший враг по прямой, без учёта тороидального экрана | ✓ |
| С учётом тороидального wrap | 9 фантомных позиций для shortest path | |
| По направлению движения | Ближайший враг в секторе перед ракетой | |

**User's choice:** [auto] Евклидово расстояние без wrap (recommended default)
**Notes:** Тороидальное наведение исключено в REQUIREMENTS.md как непропорциональная сложность.

---

## ECS-компоненты ракеты

| Option | Description | Selected |
|--------|-------------|----------|
| Переиспользовать MoveData/LifeTimeData + новые RocketTag, RocketTargetData, RocketAmmoData | Минимум новых компонентов, максимум переиспользования | ✓ |
| Монолитный RocketData со всеми полями | Один компонент вместо нескольких | |
| Полностью новые компоненты | Не переиспользовать существующие | |

**User's choice:** [auto] Переиспользовать + новые специфичные (recommended default)
**Notes:** Следует установленному паттерну: BulletTag + MoveData + LifeTimeData. Ракета аналогична пуле, но с дополнительным наведением.

---

## Боезапас и перезарядка

| Option | Description | Selected |
|--------|-------------|----------|
| Инкрементальная (как LaserSystem) | Одна ракета за период перезарядки | ✓ |
| Batch (как GunSystem) | Все ракеты разом после полной перезарядки | |
| Без перезарядки | Фиксированное количество на игру | |

**User's choice:** [auto] Инкрементальная перезарядка (recommended default)
**Notes:** Соответствует ROCK-06 "инкрементальная перезарядка". Более гранулярная обратная связь для игрока.

---

## Claude's Discretion

- Конкретная математика поворота (atan2 vs cross-product)
- Порядок EcsRocketGuidanceSystem в update chain
- Отдельная система для перезарядки боезапаса vs расширение существующей
