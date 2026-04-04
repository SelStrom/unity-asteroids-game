# Phase 2: Unity 6.3 Upgrade - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-02
**Phase:** 02-unity-6-3-upgrade
**Areas discussed:** Верификация геймплея

---

## Выбор областей для обсуждения

| Option | Description | Selected |
|--------|-------------|----------|
| Совместимость пакетов | Стратегия обновления InputSystem, UGS Auth/Leaderboards, 2D feature pack | |
| Deprecated API | Подход к поиску и замене deprecated API | |
| TMP миграция | Проверка встроенного TMP, обработка локальных TMP-ассетов | |
| Верификация геймплея | Как проверять функциональную эквивалентность 1:1 | ✓ |

**User's choice:** Верификация геймплея
**Notes:** Остальные области не выбраны — пользователь доверяет Claude в технических решениях по пакетам, API и TMP

---

## Верификация геймплея

### Как проверять геймплей 1:1?

| Option | Description | Selected |
|--------|-------------|----------|
| Ручной чеклист (Реком.) | Ручной UAT-чеклист по механикам в Editor | |
| Чеклист + EditMode тесты | Ручной чеклист + базовые EditMode-тесты на компиляцию и TMP | |
| Полный тест сюит | EditMode + PlayMode тесты покрывающие все системы | ✓ |

**User's choice:** Полный тест сюит
**Notes:** —

### Состав тестов для Phase 2?

| Option | Description | Selected |
|--------|-------------|----------|
| Компиляция + сцены (Реком.) | EditMode: компиляция, TMP-типы, конфиги. PlayMode: загрузка сцены | |
| Компиляция + геймплей цикл | EditMode + PlayMode полный цикл игры. Пересекается с TST-12 (Phase 5) | |
| Ты решай | Claude определит оптимальный состав | ✓ |

**User's choice:** Ты решай
**Notes:** Claude имеет гибкость в определении конкретного состава тестов

### Локальные TMP-ассеты

| Option | Description | Selected |
|--------|-------------|----------|
| Удалить локальные (Реком.) | Удалить Assets/TextMesh Pro/ — чистый подход | ✓ |
| Оставить как есть | Не трогать, Unity разберётся | |
| Ты решай | Claude определит | |

**User's choice:** Удалить локальные (Реком.)
**Notes:** —

### Предупреждения компилятора

| Option | Description | Selected |
|--------|-------------|----------|
| Zero warnings (Реком.) | Исправить все предупреждения в Assets/ | ✓ |
| Только ошибки | Исправить только ошибки компиляции | |
| Ты решай | Claude определит по факту | |

**User's choice:** Zero warnings (Реком.)
**Notes:** —

---

## Claude's Discretion

- Конкретный состав EditMode и PlayMode тестов
- Стратегия обновления пакетов (порядок, версии)
- Подход к поиску deprecated API
- Порядок выполнения шагов апгрейда

## Deferred Ideas

None — discussion stayed within phase scope
