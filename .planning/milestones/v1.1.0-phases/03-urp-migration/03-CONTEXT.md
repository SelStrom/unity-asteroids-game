# Phase 3: URP Migration - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Миграция рендеринга проекта Asteroids с Built-in Render Pipeline на Universal Render Pipeline (URP) с 2D Renderer. Все спрайты, частицы, UI и LineRenderer отображаются корректно. Post-Processing настроен через URP Volume. Визуальный результат соответствует оригиналу. Геймплей 1:1.

</domain>

<decisions>
## Implementation Decisions

### URP Renderer
- **D-01:** Использовать **2D Renderer** (не Universal Renderer) — проект полностью 2D, ортографическая камера, SpriteRenderer
- **D-02:** Создать URP Asset + 2D Renderer Asset, назначить в Project Settings > Graphics и Quality

### Конвертация материалов
- **D-03:** Использовать **Render Pipeline Converter** для автоматической конвертации материалов, затем ручная проверка результата
- **D-04:** Спрайты используют Sprites-Default (встроенный шейдер) — URP автоматически подхватит Sprite-Lit-Default или Sprite-Unlit-Default
- **D-05:** Кастомных материалов в проекте нет (кроме TMP в Assets/TextMesh Pro/, которые должны быть удалены по D-03 из Phase 2)

### ParticleSystem (vfx_blow)
- **D-06:** Конвертировать материал ParticleSystem на **Universal/Particles** шейдеры (Unlit или Lit в зависимости от текущего эффекта)
- **D-07:** Проверить, что ParticleSystem воспроизводится и останавливается корректно (EffectVisual.OnParticleSystemStopped callback)

### LineRenderer (лазер)
- **D-08:** Использовать **URP Unlit шейдер** для материала LineRenderer лазера (lazer.prefab)
- **D-09:** Проверить визуальное соответствие лазерного луча оригиналу (цвет, толщина, прозрачность)

### Post-Processing
- **D-10:** Настроить **URP Volume** (Global) с эффектами **Bloom** и **Vignette** — минимальный набор для визуального соответствия
- **D-11:** Интенсивность эффектов — на усмотрение Claude, ориентируясь на оригинальный визуал (классический Asteroids: тёмный фон, яркие контуры)

### Камера
- **D-12:** Сохранить текущие параметры ортографической камеры (orthographicSize = 22.5)
- **D-13:** UniversalAdditionalCameraData будет добавлен автоматически при назначении URP Pipeline
- **D-14:** Pixel Perfect Camera не требуется (игра не пиксель-арт, используются векторные спрайты)

### Claude's Discretion
- Конкретные значения Bloom threshold/intensity и Vignette intensity/smoothness
- Выбор между Sprite-Lit-Default и Sprite-Unlit-Default для спрайтов
- Порядок шагов конвертации
- Стратегия тестирования визуального соответствия (скриншоты, ручная проверка)
- Нужна ли 2D Light для освещения сцены или достаточно Unlit шейдеров

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Текущее состояние рендеринга
- `ProjectSettings/GraphicsSettings.asset` — текущий Built-in RP, shader references
- `ProjectSettings/QualitySettings.asset` — текущие Quality уровни (нужно назначить URP Asset)

### Prefabs и визуалы
- `Assets/Media/prefabs/` — 10 gameplay prefabs (ship, asteroids, bullets, ufo) с SpriteRenderer
- `Assets/Media/effects/vfx_blow.prefab` — ParticleSystem эффект взрыва
- `Assets/Media/effects/lazer.prefab` — LineRenderer лазер

### Код с рендер-зависимостями
- `Assets/Scripts/View/ShipVisual.cs` — SpriteRenderer, переключение спрайтов (thrust)
- `Assets/Scripts/View/AsteroidVisual.cs` — SpriteRenderer
- `Assets/Scripts/View/EffectVisual.cs` — ParticleSystem, OnParticleSystemStopped callback
- `Assets/Scripts/Application/Game.cs:212` — LineRenderer для лазера
- `Assets/Scripts/Application/Application.cs:37-39` — Camera.main, orthographicSize

### Phase 2 контекст
- `.planning/phases/02-unity-6-3-upgrade/02-CONTEXT.md` — решения по TMP, zero warnings

### Требования
- `.planning/REQUIREMENTS.md` §URP Migration — URP-01..URP-06

### Анализ кодовой базы
- `.planning/codebase/STACK.md` — текущий стек, Built-in RP
- `.planning/codebase/STRUCTURE.md` — структура файлов
- `.planning/codebase/INTEGRATIONS.md` — интеграции

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Assets/Tests/EditMode/` и `Assets/Tests/PlayMode/` — тестовые assemblies уже настроены (Phase 1/2)
- `Assets/Tests/EditMode/Upgrade/` — тесты апгрейда Phase 2, паттерн для URP-тестов

### Established Patterns
- SpriteRenderer используется в ShipVisual и AsteroidVisual через `_spriteRenderer.sprite = sprite` (простое назначение, не зависит от шейдера)
- ParticleSystem в EffectVisual с callback `OnParticleSystemStopped` — важно сохранить работоспособность callback
- LineRenderer для лазера получается через пул: `_catalog.ViewFactory.Get<LineRenderer>(_configs.Laser.Prefab)`
- Камера: `Camera.main` с `orthographicSize` и `aspect` для расчёта GameArea — не зависит от render pipeline

### Integration Points
- `ProjectSettings/GraphicsSettings.asset` — замена render pipeline
- `ProjectSettings/QualitySettings.asset` — назначение URP Asset
- Все prefabs с SpriteRenderer — автоматическая конвертация материалов
- `Assets/Media/effects/` — ручная проверка материалов частиц и линий

</code_context>

<specifics>
## Specific Ideas

- Проект минималистичный — нет кастомных шейдеров и материалов, конвертация должна быть прямолинейной
- 2D Renderer — единственный разумный выбор для этого типа проекта
- Post-Processing через Volume — стандартный подход URP, Bloom подчеркнёт стиль классического Asteroids
- Assets/TextMesh Pro/ всё ещё присутствует как untracked файлы — должен быть удалён (решение D-03 из Phase 2)

</specifics>

<deferred>
## Deferred Ideas

- **2D Lighting** (динамическое освещение, тени от спрайтов) — отложено на v2 (VIS-01), не часть миграции 1:1
- **Pixel Perfect Camera** — не требуется для текущего визуала

</deferred>

---

*Phase: 03-urp-migration*
*Context gathered: 2026-04-02*
