# Phase 15: HUD - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md -- this log preserves the alternatives considered.

**Date:** 2026-04-06
**Phase:** 15-hud
**Mode:** auto
**Areas discussed:** Формат отображения, Bridge integration, UI Layout, MCP-верификация

---

## Формат отображения

| Option | Description | Selected |
|--------|-------------|----------|
| Текстовый формат (как Laser) | "Rockets: N", "Reload rocket: N sec" -- единообразно с существующим HUD | [auto] |
| Графический формат (иконки) | Иконки ракет вместо текста -- визуально нагляднее, но требует новый паттерн | |
| Прогресс-бар | Заполняемая полоска для таймера -- современнее, но не совпадает со стилем | |

**User's choice:** [auto] Текстовый формат по аналогии с лазером
**Notes:** Recommended default -- единообразие с существующим HUD (Laser shoots / Reload laser)

---

## Bridge integration

| Option | Description | Selected |
|--------|-------------|----------|
| Расширить ObservableBridgeSystem | Добавить RocketAmmoData в существующий Query -- минимальное изменение | [auto] |
| Отдельная система | Создать RocketHudBridgeSystem -- изоляция, но лишняя система | |

**User's choice:** [auto] Расширить ObservableBridgeSystem
**Notes:** Recommended default -- паттерн уже установлен для LaserData

---

## UI Layout

| Option | Description | Selected |
|--------|-------------|----------|
| Под Laser полями | Rocket info под существующими Laser строками -- логическая группировка оружия | [auto] |
| Отдельная секция | Rocket info в отдельном блоке HUD | |

**User's choice:** [auto] Под Laser полями
**Notes:** Recommended default -- оружейная информация вместе

---

## MCP-верификация

| Option | Description | Selected |
|--------|-------------|----------|
| Game View screenshot + визуальная проверка | Скриншот в PlayMode, подтверждение наличия и корректности HUD элементов | [auto] |

**User's choice:** [auto] Game View screenshot
**Notes:** Recommended default -- соответствует TEST-03 requirement

---

## Claude's Discretion

- Точное расположение в HUD (ниже laser)
- Размер шрифта, цвет TMP_Text
- Форматирование таймера (целые vs десятичные секунды)

## Deferred Ideas

None -- all auto-resolved within phase scope
