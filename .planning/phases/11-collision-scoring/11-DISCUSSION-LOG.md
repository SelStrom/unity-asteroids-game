# Phase 11: Collision & Scoring - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md -- this log preserves the alternatives considered.

**Date:** 2026-04-05
**Phase:** 11-collision-scoring
**Mode:** auto
**Areas discussed:** Обработка коллизии ракеты, Начисление очков, Scope коллизий, Дробление астероидов, Тестирование

---

## Обработка коллизии ракеты

| Option | Description | Selected |
|--------|-------------|----------|
| Аналогично PlayerBullet | Ракета убивает врага + получает DeadTag + начисляет очки. Паттерн уже есть в ProcessCollision | ✓ |
| Отдельная логика | Своя ветка обработки с другими правилами (например, ракета проходит сквозь врага) | |
| Через отдельную систему | Новая EcsRocketCollisionSystem вместо расширения существующей | |

**User's choice:** [auto] Аналогично PlayerBullet (recommended default)
**Notes:** Паттерн ProcessCollision уже обрабатывает PlayerBullet+Enemy и Ship+Enemy. Ракета -- третий тип снаряда игрока.

---

## Начисление очков

| Option | Description | Selected |
|--------|-------------|----------|
| Те же ScoreValue | Очки за ракету = очки за пулю, берутся из ScoreValue на enemy entity | ✓ |
| Множитель для ракеты | ScoreValue * коэффициент (бонус или штраф за использование ракеты) | |

**User's choice:** [auto] Те же ScoreValue (recommended default)
**Notes:** ScoreValue привязан к типу врага, не к типу снаряда. Это правильно -- ценность цели не зависит от оружия.

---

## Scope коллизий ракеты

| Option | Description | Selected |
|--------|-------------|----------|
| Только враги | Ракета коллидирует только с AsteroidTag, UfoBigTag, UfoTag | ✓ |
| Враги + корабль | Ракета может случайно убить игрока | |
| Любые entity | Ракета коллидирует со всеми (пули, корабль, другие ракеты) | |

**User's choice:** [auto] Только враги (recommended default)
**Notes:** Requirement COLL-03: "Ракета уничтожается при столкновении с любым врагом" -- подтверждает scope только врагов.

---

## Дробление астероидов

| Option | Description | Selected |
|--------|-------------|----------|
| Вызывает дробление | DeadTag запускает стандартную логику дробления в Bridge | ✓ |
| Полное уничтожение | Ракета уничтожает астероид без дробления | |

**User's choice:** [auto] Вызывает дробление (recommended default)
**Notes:** Success criteria: "дробление работает". Дробление управляется AsteroidAge при обработке DeadTag.

---

## Тестирование

| Option | Description | Selected |
|--------|-------------|----------|
| Расширить CollisionHandlerTests | Добавить test methods в существующий файл | ✓ |
| Отдельный RocketCollisionTests | Новый файл тестов только для ракет | |

**User's choice:** [auto] Расширить CollisionHandlerTests (recommended default)
**Notes:** Ракетные коллизии -- это часть EcsCollisionHandlerSystem. Тесты в том же файле логичны.

---

## Claude's Discretion

- Порядок проверок в ProcessCollision
- Helper CreateRocketEntity в test fixture
- Assert messages

## Deferred Ideas

None
