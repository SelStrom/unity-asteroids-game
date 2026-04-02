# Asteroids

## What This Is

Классическая аркадная игра Asteroids на Unity. Корабль, астероиды, НЛО, стрельба пулями и лазером, тороидальный экран, лидерборд через Unity Gaming Services. Текущая реализация — ECS-подобная архитектура на MonoBehaviour + MVVM (shtl-mvvm) для UI.

## Core Value

Играбельная классическая механика Asteroids с онлайн-лидербордом — фундамент для технической миграции на современный стек Unity.

## Requirements

### Validated

- ✓ Управление кораблём (тяга, поворот, инерция) — existing
- ✓ Стрельба пулями с перезарядкой — existing
- ✓ Лазерная атака с зарядами — existing
- ✓ Астероиды с фрагментацией (big → medium → small) — existing
- ✓ НЛО двух типов (big/small) с AI-наведением — existing
- ✓ Тороидальный экран (обёртывание по краям) — existing
- ✓ Подсчёт очков и система жизней — existing
- ✓ Анонимная аутентификация через UGS — existing
- ✓ Онлайн-лидерборд с именами игроков — existing
- ✓ Экраны: Title, Game HUD, Result — existing
- ✓ WebGL и Windows standalone сборки — existing

### Active

- ✓ Апгрейд на Unity 6.3 с адаптацией к встроенному TMP — Validated in Phase 2: Unity 6.3 Upgrade
- ✓ Фикс shtl-mvvm для совместимости с Unity 6.3 (TMP как внутренний модуль) и обратной совместимости с Unity 2022.3+ — Validated in Phase 1: Dev Tooling + shtl-mvvm Fix
- [ ] Миграция с Built-in Render Pipeline на URP
- [ ] Переход геймплейной логики на гибридный DOTS (Entities для логики/физики, GameObjects для UI/визуала)

### Out of Scope

- Исправление существующих багов (7 критических из анализа) — не входит в scope миграции, функционал 1:1
- Новые игровые механики — только миграция существующего функционала
- Полный DOTS (без GameObjects) — выбран гибридный подход

## Current Milestone: v1.0 Техническая миграция

**Goal:** Перевести проект на современный стек Unity 6.3 + URP + гибридный DOTS, сохранив весь текущий функционал 1:1.

**Target features:**
1. Апгрейд на Unity 6.3 (включая TMP-совместимость и фикс shtl-mvvm)
2. Миграция на Universal Render Pipeline
3. Гибридный DOTS для геймплейной логики

## Context

- **Движок:** Unity 2022.3.60f1 LTS, C# 9.0, Mono backend
- **Архитектура:** ECS-подобная на MonoBehaviour (8 систем, 5 типов сущностей, visitor-паттерн), MVVM через shtl-mvvm
- **shtl-mvvm:** Собственная библиотека пользователя (github.com/SelStrom/shtl-mvvm), подключена через git. Имеет явную зависимость на com.unity.textmeshpro — требуется фикс для Unity 6.3
- **Render:** Built-in Render Pipeline, 2D, ортографическая камера (size 22.5)
- **UI:** uGUI + TextMeshPro + shtl-mvvm bindings
- **Кодовая база:** ~50 C# файлов, ~2200 LOC, 0 тестов
- **Платформы:** Editor, WebGL, WindowsStandalone64

## Constraints

- **Порядок миграции:** Unity 6.3 → URP → DOTS — каждый шаг на стабильной базе предыдущего
- **Обратная совместимость shtl-mvvm:** Фикс TMP должен работать начиная с Unity 2022.3
- **Функциональная эквивалентность:** Геймплей 1:1 после каждого этапа миграции
- **Гибридный DOTS:** Entities для логики/физики, GameObjects для UI и визуала

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Unity 6.3 первым шагом | Апгрейд редактора — базовое требование для URP и DOTS | — Pending |
| Гибридный DOTS вместо полного | UI на GameObjects проще поддерживать с shtl-mvvm | — Pending |
| Баги out of scope | Миграция 1:1, исправления в отдельном milestone | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-02 after initialization*
