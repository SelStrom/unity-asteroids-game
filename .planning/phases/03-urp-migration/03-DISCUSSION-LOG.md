# Phase 3: URP Migration - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-02
**Phase:** 03-urp-migration
**Areas discussed:** URP Renderer, конвертация материалов, ParticleSystem, LineRenderer (лазер), Post-Processing, камера
**Mode:** --auto (all decisions auto-selected)

---

## URP Renderer

| Option | Description | Selected |
|--------|-------------|----------|
| 2D Renderer | Стандартный для 2D-игр, поддержка SpriteRenderer, 2D Lighting | ✓ |
| Universal Renderer | Для 3D-сцен, избыточен для 2D проекта | |

**User's choice:** [auto] 2D Renderer (рекомендовано для 2D-игры)
**Notes:** Проект полностью 2D, ортографическая камера, только SpriteRenderer

---

## Конвертация материалов

| Option | Description | Selected |
|--------|-------------|----------|
| Render Pipeline Converter + ручная проверка | Автоматическая конвертация с последующей верификацией | ✓ |
| Полностью ручная конвертация | Создание новых материалов вручную | |

**User's choice:** [auto] Render Pipeline Converter + ручная проверка (рекомендовано)
**Notes:** Нет кастомных материалов — конвертация тривиальна. Sprites-Default → URP Sprite шейдер

---

## ParticleSystem (vfx_blow)

| Option | Description | Selected |
|--------|-------------|----------|
| Universal/Particles шейдеры | Стандартные URP шейдеры для частиц (Unlit/Lit) | ✓ |
| Кастомный шейдер | Написать свой шейдер для частиц | |

**User's choice:** [auto] Universal/Particles шейдеры (рекомендовано для URP)
**Notes:** Один prefab vfx_blow с ParticleSystem, callback OnParticleSystemStopped должен работать

---

## LineRenderer (лазер)

| Option | Description | Selected |
|--------|-------------|----------|
| URP Unlit шейдер | Простой шейдер без освещения для LineRenderer | ✓ |
| URP Lit шейдер | Шейдер с освещением (избыточно для линии лазера) | |

**User's choice:** [auto] URP Unlit шейдер (рекомендовано)
**Notes:** Лазер — яркая линия, освещение не нужно. lazer.prefab

---

## Post-Processing

| Option | Description | Selected |
|--------|-------------|----------|
| Bloom + Vignette через URP Volume | Минимальный набор для визуального стиля | ✓ |
| Без Post-Processing | Пропустить, добавить позже | |
| Полный набор эффектов | Bloom + Vignette + ChromaticAberration и т.д. | |

**User's choice:** [auto] Bloom + Vignette (по требованиям URP-04)
**Notes:** Классический Asteroids — тёмный фон, яркие контуры, Bloom подчеркнёт стиль

---

## Камера

| Option | Description | Selected |
|--------|-------------|----------|
| Сохранить параметры + UniversalAdditionalCameraData | Минимальные изменения камеры | ✓ |
| Pixel Perfect Camera | Добавить для чёткости спрайтов | |

**User's choice:** [auto] Сохранить текущие параметры (рекомендовано)
**Notes:** orthographicSize = 22.5, не пиксель-арт, Pixel Perfect не нужен

---

## Claude's Discretion

- Конкретные значения Bloom/Vignette параметров
- Выбор Sprite-Lit vs Sprite-Unlit
- Порядок шагов конвертации
- Стратегия визуального тестирования

## Deferred Ideas

- 2D Lighting (VIS-01) — v2
- Pixel Perfect Camera — не требуется
