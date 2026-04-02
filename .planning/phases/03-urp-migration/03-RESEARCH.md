# Phase 3: URP Migration - Research

**Researched:** 2026-04-02
**Domain:** Unity Universal Render Pipeline (URP) 2D Renderer, миграция с Built-in RP
**Confidence:** HIGH

## Summary

Миграция проекта Asteroids с Built-in Render Pipeline на URP с 2D Renderer -- прямолинейная процедура. Проект не содержит кастомных материалов и шейдеров: все SpriteRenderer используют встроенный `Sprites-Default` (fileID: 10754), LineRenderer лазера использует `Default-Line` (fileID: 10306), ParticleSystem использует `Default-Particle` (fileID: 10308). Кастомных материалов нет. Это значительно упрощает миграцию -- Render Pipeline Converter конвертирует встроенные материалы автоматически, а для LineRenderer и ParticleSystem потребуется ручное назначение URP-совместимых шейдеров.

Unity 6.3 LTS (6000.3.x) включает URP 17.5.x через Unity Registry. Пакет `com.unity.render-pipelines.universal` устанавливается через Package Manager. 2D Renderer Asset создается через контекстное меню и назначается в Project Settings. Post-Processing (Bloom, Vignette) настраивается через URP Volume на камере.

**Primary recommendation:** Установить URP через Package Manager, создать URP Asset (with 2D Renderer), запустить Render Pipeline Converter для 2D материалов, вручную исправить материалы LineRenderer и ParticleSystem, настроить Global Volume с Bloom и Vignette.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Использовать **2D Renderer** (не Universal Renderer) -- проект полностью 2D, ортографическая камера, SpriteRenderer
- **D-02:** Создать URP Asset + 2D Renderer Asset, назначить в Project Settings > Graphics и Quality
- **D-03:** Использовать **Render Pipeline Converter** для автоматической конвертации материалов, затем ручная проверка результата
- **D-04:** Спрайты используют Sprites-Default (встроенный шейдер) -- URP автоматически подхватит Sprite-Lit-Default или Sprite-Unlit-Default
- **D-05:** Кастомных материалов в проекте нет (кроме TMP в Assets/TextMesh Pro/, которые должны быть удалены по D-03 из Phase 2)
- **D-06:** Конвертировать материал ParticleSystem на **Universal/Particles** шейдеры (Unlit или Lit в зависимости от текущего эффекта)
- **D-07:** Проверить, что ParticleSystem воспроизводится и останавливается корректно (EffectVisual.OnParticleSystemStopped callback)
- **D-08:** Использовать **URP Unlit шейдер** для материала LineRenderer лазера (lazer.prefab)
- **D-09:** Проверить визуальное соответствие лазерного луча оригиналу (цвет, толщина, прозрачность)
- **D-10:** Настроить **URP Volume** (Global) с эффектами **Bloom** и **Vignette** -- минимальный набор для визуального соответствия
- **D-11:** Интенсивность эффектов -- на усмотрение Claude, ориентируясь на оригинальный визуал (классический Asteroids: темный фон, яркие контуры)
- **D-12:** Сохранить текущие параметры ортографической камеры (orthographicSize = 22.5)
- **D-13:** UniversalAdditionalCameraData будет добавлен автоматически при назначении URP Pipeline
- **D-14:** Pixel Perfect Camera не требуется (игра не пиксель-арт, используются векторные спрайты)

### Claude's Discretion
- Конкретные значения Bloom threshold/intensity и Vignette intensity/smoothness
- Выбор между Sprite-Lit-Default и Sprite-Unlit-Default для спрайтов
- Порядок шагов конвертации
- Стратегия тестирования визуального соответствия (скриншоты, ручная проверка)
- Нужна ли 2D Light для освещения сцены или достаточно Unlit шейдеров

### Deferred Ideas (OUT OF SCOPE)
- **2D Lighting** (динамическое освещение, тени от спрайтов) -- отложено на v2 (VIS-01), не часть миграции 1:1
- **Pixel Perfect Camera** -- не требуется для текущего визуала
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| URP-01 | URP пакет установлен, 2D Renderer Asset создан и назначен | Стандартная процедура: Package Manager > Universal RP, Create > Rendering > URP Asset (with 2D Renderer), назначение в Graphics + Quality Settings |
| URP-02 | Render Pipeline Converter выполнен, все материалы конвертированы | Window > Rendering > Render Pipeline Converter, тип "Built-In 2D to URP 2D". Встроенные материалы (Sprites-Default, Default-Line, Default-Particle) требуют конвертации |
| URP-03 | ParticleSystem материалы адаптированы под URP | vfx_blow.prefab использует Default-Particle (fileID: 10308). Нужен шейдер Universal Render Pipeline/Particles/Unlit |
| URP-04 | URP Volume с базовым Post-Processing настроен (Bloom, Vignette) | Global Volume на сцене, Volume Profile с Bloom + Vignette override, Post Processing checkbox на камере |
| URP-05 | Визуальный результат соответствует оригиналу (спрайты, частицы, UI) | Sprite-Unlit-Default для спрайтов (без освещения -- 1:1 с оригиналом), ручная проверка всех 10 gameplay prefabs |
| URP-06 | Игра запускается в Editor и воспроизводит весь геймплей 1:1 | Код не зависит от render pipeline (SpriteRenderer.sprite, LineRenderer, ParticleSystem API идентичны) |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Язык:** Документация и комментарии на русском
- **Фигурные скобки:** Обязательны для всех if/else/for/while даже с одной строкой
- **Порядок миграции:** Unity 6.3 (done) -> URP (this phase) -> DOTS
- **Функциональная эквивалентность:** Геймплей 1:1 после каждого этапа миграции
- **C# 9.0**, .NET Standard 2.1, unsafe запрещен
- **GSD Workflow:** Все изменения через GSD команды

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.render-pipelines.universal | 17.5.x (Unity Registry) | URP рендеринг | Единственный SRP в Unity 6 для 2D, устанавливается из Unity Registry |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| (нет дополнительных пакетов) | - | - | URP подтягивает все зависимости (core, shader-graph) автоматически |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| 2D Renderer | Universal Renderer | Universal Renderer для 3D; 2D Renderer -- единственный правильный выбор для 2D ортографической игры |
| Sprite-Unlit-Default | Sprite-Lit-Default | Lit требует 2D Light на сцене для видимости; Unlit рендерит без освещения -- точное 1:1 с Built-in Sprites-Default |

**Installation:**
```
# Через Unity Package Manager (UI):
# Window > Package Management > Package Manager > Unity Registry > Universal RP > Install
#
# Или в manifest.json:
"com.unity.render-pipelines.universal": "17.5.0"
```

**Version verification:** URP версия привязана к Unity 6.3 и устанавливается из Unity Registry. Точную версию определяет Package Manager автоматически. По данным документации Unity 6000.3.x использует URP 17.5.x.

## Architecture Patterns

### Рекомендуемая структура ассетов для URP
```
Assets/
  Settings/
    URP-2D-Asset.asset          # URP Asset (2D Renderer)
    URP-2D-Renderer.asset       # 2D Renderer Data
    URP-PostProcessing.asset    # Volume Profile (Bloom, Vignette)
  Scenes/
    Main.unity                  # Сцена с Global Volume на камере
  Media/
    effects/
      lazer.prefab              # LineRenderer с URP Unlit материалом
      vfx_blow.prefab           # ParticleSystem с URP/Particles/Unlit материалом
    prefabs/                    # Gameplay prefabs (SpriteRenderer -- автоконвертация)
```

### Pattern 1: URP Asset + 2D Renderer
**What:** URP Asset ссылается на 2D Renderer Data Asset. Оба создаются через Create > Rendering > URP Asset (with 2D Renderer).
**When to use:** Всегда для 2D проектов.
**Details:**
- URP Asset назначается в Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings
- URP Asset назначается в Edit > Project Settings > Quality > каждый Quality Level > Render Pipeline Asset
- В QualitySettings.asset все 6 уровней (Very Low..Ultra) должны получить ссылку на URP Asset через поле `customRenderPipeline`

### Pattern 2: Global Volume для Post-Processing
**What:** GameObject с компонентом Volume (Mode: Global) и Volume Profile содержащим Bloom + Vignette overrides.
**When to use:** Для применения post-processing эффектов ко всей сцене.
**Details:**
- Volume добавляется на отдельный GameObject на сцене (или на камеру)
- Volume Profile -- ScriptableObject с настройками эффектов
- На камере необходимо включить checkbox "Post Processing"
- Bloom: threshold ~0.8, intensity ~0.5-1.0 (яркие белые контуры на черном фоне)
- Vignette: intensity ~0.3-0.4, smoothness ~0.5 (затемнение по краям)

### Pattern 3: Конвертация материалов через Render Pipeline Converter
**What:** Автоматическая конвертация встроенных Built-in материалов в URP эквиваленты.
**When to use:** При первичной миграции с Built-in RP на URP.
**Details:**
- Window > Rendering > Render Pipeline Converter
- Тип конвертации: "Built-In Render Pipeline 2D to URP 2D"
- Initialize Converters > Convert Assets
- Конвертирует ссылки на встроенные материалы в prefabs и сцене

### Anti-Patterns to Avoid
- **Ручная замена материалов в каждом prefab:** Используй Render Pipeline Converter для массовой конвертации, ручная работа только для special cases (LineRenderer, ParticleSystem)
- **Назначение URP Asset только в Graphics Settings:** НЕОБХОДИМО также назначить во ВСЕХ Quality Level в Quality Settings, иначе при переключении уровня качества будет fallback на Built-in
- **Использование Sprite-Lit-Default без 2D Light:** Спрайты будут невидимы (черные) без источника света. Для миграции 1:1 использовать Sprite-Unlit-Default
- **Забыть включить Post Processing на камере:** Volume настроен, но эффекты не отображаются -- нужен checkbox на Camera компоненте

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Конвертация материалов | Скрипт замены шейдеров | Render Pipeline Converter | Обрабатывает встроенные материалы, ссылки в prefabs и сцене |
| Post-Processing эффекты | Custom image effects | URP Volume с Bloom/Vignette | Встроено в URP, оптимизировано, поддерживается |
| Создание URP ассетов | Ручная настройка через код | Create > Rendering > URP Asset (with 2D Renderer) | Автоматически связывает URP Asset с 2D Renderer Data |

**Key insight:** Проект настолько минималистичен (нет кастомных материалов/шейдеров), что вся миграция сводится к: установка пакета, создание ассетов, запуск конвертера, ручная правка 2 special-case prefabs.

## Common Pitfalls

### Pitfall 1: Quality Settings не обновлены
**What goes wrong:** URP назначен в Graphics Settings, но Quality Settings имеют `customRenderPipeline: {fileID: 0}` -- при запуске рендерится Built-in RP
**Why it happens:** В QualitySettings.asset 6 уровней качества, каждый со своим полем `customRenderPipeline`
**How to avoid:** Назначить URP Asset во ВСЕХ Quality Level через Project Settings > Quality. Текущий активный уровень -- Ultra (m_CurrentQuality: 5)
**Warning signs:** Розовые материалы только при определенном Quality Level, или наоборот -- нет розовых материалов, но нет и URP-эффектов

### Pitfall 2: LineRenderer Default-Line шейдер не конвертируется
**What goes wrong:** Render Pipeline Converter может не конвертировать встроенный Default-Line материал (fileID: 10306), лазер отображается розовым
**Why it happens:** Default-Line -- встроенный Built-in материал, не все встроенные материалы покрываются конвертером для 2D
**How to avoid:** Создать новый материал с шейдером Universal Render Pipeline/Unlit и назначить его в lazer.prefab вручную. Цвет: белый (как в оригинале -- gradient key0 и key1 = {r:1, g:1, b:1, a:1})
**Warning signs:** Розовый лазерный луч в Game View

### Pitfall 3: ParticleSystem Default-Particle шейдер
**What goes wrong:** vfx_blow.prefab отображает розовые частицы
**Why it happens:** Default-Particle (fileID: 10308) -- встроенный материал, может не конвертироваться автоматически
**How to avoid:** Создать новый материал с шейдером Universal Render Pipeline/Particles/Unlit. Настроить Surface Type: Transparent, Blending Mode по оригиналу
**Warning signs:** Розовые частицы взрыва

### Pitfall 4: Post-Processing не отображается
**What goes wrong:** Global Volume настроен, но Bloom/Vignette не видны в Game View
**Why it happens:** На Camera компоненте не включен checkbox "Post Processing"
**How to avoid:** Выбрать Main Camera, в Inspector найти Camera > Rendering > Post Processing = true
**Warning signs:** Volume Profile настроен, эффекты видны в Scene View, но не в Game View

### Pitfall 5: OnParticleSystemStopped перестает вызываться
**What goes wrong:** EffectVisual.OnParticleSystemStopped callback не срабатывает после миграции, эффекты взрыва не возвращаются в пул
**Why it happens:** ParticleSystem stopAction = 3 (Callback) зависит от корректного материала и рендеринга
**How to avoid:** Проверить что stopAction остается = 3, и что ParticleSystem.Play()/Stop() работают корректно после замены материала
**Warning signs:** Утечка объектов из пула, эффекты взрыва накапливаются на сцене

### Pitfall 6: UI (uGUI/TMP) отображается некорректно
**What goes wrong:** UI элементы (HUD, Score, Leaderboard) становятся невидимы или меняют внешний вид
**Why it happens:** Canvas Render Mode = Screen Space - Camera (m_RenderMode: 1 в Main.unity), привязан к Main Camera
**How to avoid:** Проверить Canvas после миграции -- Screen Space Camera mode должен работать с URP. UI материалы обычно не требуют конвертации (uGUI/TMP используют собственные шейдеры, совместимые с URP)
**Warning signs:** UI исчезает или накладывается неправильно

## Code Examples

### Текущее состояние файлов (что нужно изменить)

#### GraphicsSettings.asset -- текущее состояние (Built-in RP)
```yaml
# Текущее: m_CustomRenderPipeline: {fileID: 0}
# Нужно:  m_CustomRenderPipeline: {fileID: ..., guid: <URP-Asset-GUID>}
```

#### QualitySettings.asset -- текущее состояние (все 6 уровней)
```yaml
# Текущее для каждого из 6 Quality Level:
# customRenderPipeline: {fileID: 0}
# Нужно для каждого:
# customRenderPipeline: {fileID: ..., guid: <URP-Asset-GUID>}
```

#### lazer.prefab -- текущий материал
```yaml
# Текущее: {fileID: 10306, guid: 0000000000000000f000000000000000, type: 0}
# (Default-Line built-in material)
# Нужно: ссылка на новый URP Unlit материал
```

#### vfx_blow.prefab -- текущий материал (ParticleSystemRenderer)
```yaml
# Текущее: {fileID: 10308, guid: 0000000000000000f000000000000000, type: 0}
# (Default-Particle built-in material)
# Нужно: ссылка на новый URP/Particles/Unlit материал
```

### Рекомендация по выбору шейдеров (Claude's Discretion)

| Компонент | Текущий шейдер | URP шейдер | Обоснование |
|-----------|---------------|------------|-------------|
| SpriteRenderer (все prefabs) | Sprites-Default | Sprite-Unlit-Default | Unlit = 1:1 с оригиналом, не требует 2D Light (отложено на VIS-01) |
| LineRenderer (lazer) | Default-Line | Universal Render Pipeline/Unlit | Белый луч без освещения, совпадает с оригиналом |
| ParticleSystem (vfx_blow) | Default-Particle | Universal Render Pipeline/Particles/Unlit | Частицы без освещения |
| UI (TMP, uGUI) | Встроенные TMP/UI шейдеры | Не требуют конвертации | TMP и uGUI совместимы с URP из коробки |

### Рекомендация по Post-Processing (Claude's Discretion)

```
Bloom:
  Threshold: 0.9        # Только самые яркие пиксели (белые спрайты на черном фоне)
  Intensity: 0.5        # Умеренный glow, подчеркивает контуры в стиле классического Asteroids
  Scatter: 0.7          # Средний разброс свечения

Vignette:
  Color: Black (0,0,0)
  Center: (0.5, 0.5)
  Intensity: 0.35       # Легкое затемнение краев, фокус на центр экрана
  Smoothness: 0.4       # Плавный переход

2D Light: НЕ НУЖНА -- Sprite-Unlit-Default не использует освещение.
Динамическое 2D Lighting отложено на VIS-01 (v2).
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Post Processing Stack v2 (отдельный пакет) | URP Volume (встроен в URP) | URP 7.x+ | Не нужен отдельный пакет, Volume -- стандартный механизм |
| Render Pipeline Converter (menu Edit > RP) | Window > Rendering > Render Pipeline Converter | Unity 2021+ | Полноценный инструмент с preview и выбором конвертеров |
| Render Graph (opt-in) | Render Graph (default) | Unity 6 / URP 17 | Render Graph включен по умолчанию в Unity 6, но не влияет на базовую миграцию |

**Deprecated/outdated:**
- Post Processing Stack v2 (com.unity.postprocessing): заменен встроенным Volume в URP
- `Edit > Render Pipeline > Upgrade Project Materials`: заменен на Render Pipeline Converter

## Open Questions

1. **Точная версия URP для Unity 6.3 LTS**
   - What we know: URP 17.5.x совместим с Unity 6000.3.x по документации
   - What's unclear: Точный minor version (17.5.0 или 17.5.x) определится при установке
   - Recommendation: Установить через Unity Registry, Package Manager определит версию автоматически

2. **Конвертирует ли Render Pipeline Converter встроенные Default-Line и Default-Particle**
   - What we know: Конвертер для "Built-In 2D to URP 2D" обрабатывает Materials и Material References
   - What's unclear: Покрываются ли built-in materials с fileID ссылками (не .mat файлы)
   - Recommendation: Запустить конвертер, проверить результат. Если LineRenderer/ParticleSystem остались розовыми -- создать URP материалы вручную

## Environment Availability

> Фаза зависит только от Unity Editor и URP пакета из Unity Registry. Внешних инструментов не требуется.

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity Editor | Вся фаза | -- | 6.3 LTS (6000.3.x) | -- |
| URP (Unity Registry) | URP-01 | -- | 17.5.x | -- |

**Примечание:** Unity Editor запускается пользователем вручную. URP устанавливается из Unity Registry внутри редактора. Проверка доступности URP в Registry не требует CLI-инструментов. Все манипуляции с ассетами (создание URP Asset, Volume Profile, назначение материалов) выполняются через Unity Editor API или ручную правку YAML файлов.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | NUnit (com.unity.test-framework 1.6.0) |
| Config file | Assets/Tests/EditMode/*.asmdef, Assets/Tests/PlayMode/*.asmdef |
| Quick run command | Unity Editor > Window > General > Test Runner > EditMode > Run All |
| Full suite command | Unity Editor > Window > General > Test Runner > All > Run All |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| URP-01 | URP пакет установлен, 2D Renderer Asset существует | unit (EditMode) | `Unity -runTests -testFilter UrpSetupTests` | Wave 0 |
| URP-02 | GraphicsSettings и QualitySettings ссылаются на URP Asset | unit (EditMode) | `Unity -runTests -testFilter UrpSetupTests` | Wave 0 |
| URP-03 | ParticleSystem материалы используют URP шейдеры | unit (EditMode) | `Unity -runTests -testFilter UrpMaterialTests` | Wave 0 |
| URP-04 | Volume Profile существует с Bloom и Vignette | unit (EditMode) | `Unity -runTests -testFilter UrpPostProcessingTests` | Wave 0 |
| URP-05 | Визуальный результат соответствует оригиналу | manual-only | Ручная проверка в Game View | N/A (UAT) |
| URP-06 | Игра запускается и геймплей 1:1 | smoke (PlayMode) | `Unity -runTests -testFilter GameplaySmokeTests` | Существует (Phase 2) |

### Sampling Rate
- **Per task commit:** EditMode тесты через Test Runner
- **Per wave merge:** Полный набор EditMode + PlayMode тестов
- **Phase gate:** Все тесты зеленые + ручная проверка визуала (URP-05)

### Wave 0 Gaps
- [ ] `Assets/Tests/EditMode/Upgrade/UrpSetupTests.cs` -- проверка URP Asset, Graphics/Quality Settings (URP-01, URP-02)
- [ ] `Assets/Tests/EditMode/Upgrade/UrpMaterialTests.cs` -- проверка материалов prefabs на URP-совместимость (URP-03)
- [ ] `Assets/Tests/EditMode/Upgrade/UrpPostProcessingTests.cs` -- проверка Volume Profile с Bloom/Vignette (URP-04)

## Inventory проекта (текущее состояние)

### Файлы, требующие изменений

| Файл | Текущее состояние | Действие |
|------|-------------------|----------|
| `Packages/manifest.json` | Нет URP | Добавить `com.unity.render-pipelines.universal` |
| `ProjectSettings/GraphicsSettings.asset` | `m_CustomRenderPipeline: {fileID: 0}` | Назначить URP Asset |
| `ProjectSettings/QualitySettings.asset` | Все 6 уровней `customRenderPipeline: {fileID: 0}` | Назначить URP Asset во всех 6 уровнях |
| `Assets/Scenes/Main.unity` | Camera без URP данных, нет Volume | Добавить Volume, включить Post Processing на камере |
| `Assets/Media/effects/lazer.prefab` | LineRenderer с Default-Line (fileID: 10306) | Новый URP Unlit материал |
| `Assets/Media/effects/vfx_blow.prefab` | ParticleSystemRenderer с Default-Particle (fileID: 10308) | Новый URP/Particles/Unlit материал |
| 5 gameplay prefabs (ship, asteroids, bullets, ufo) | SpriteRenderer с Sprites-Default (fileID: 10754) | Render Pipeline Converter -> Sprite-Unlit-Default |
| 2 GUI prefabs (gui_text, leaderboard_entry) | TMP материалы (m_Material: {fileID: 0}) | Не требуют конвертации |

### Код, НЕ требующий изменений

| Файл | Причина |
|------|---------|
| `Assets/Scripts/View/ShipVisual.cs` | SpriteRenderer API идентичен в URP |
| `Assets/Scripts/View/AsteroidVisual.cs` | SpriteRenderer API идентичен в URP |
| `Assets/Scripts/View/EffectVisual.cs` | ParticleSystem API идентичен в URP |
| `Assets/Scripts/Application/Game.cs` | LineRenderer API идентичен в URP, Camera.main работает |
| `Assets/Scripts/Application/Application.cs` | Camera.orthographicSize, Camera.aspect -- без изменений |

### Новые файлы

| Файл | Назначение |
|------|-----------|
| `Assets/Settings/URP-2D-Asset.asset` | URP Pipeline Asset |
| `Assets/Settings/URP-2D-Renderer.asset` | 2D Renderer Data |
| `Assets/Settings/PostProcessing-Profile.asset` | Volume Profile (Bloom + Vignette) |
| `Assets/Media/materials/Laser-URP.mat` | Материал для LineRenderer (URP/Unlit) |
| `Assets/Media/materials/Particle-URP.mat` | Материал для ParticleSystem (URP/Particles/Unlit) |
| `Assets/Tests/EditMode/Upgrade/UrpSetupTests.cs` | Тесты настройки URP |
| `Assets/Tests/EditMode/Upgrade/UrpMaterialTests.cs` | Тесты материалов |
| `Assets/Tests/EditMode/Upgrade/UrpPostProcessingTests.cs` | Тесты Post-Processing |

## Sources

### Primary (HIGH confidence)
- [Unity 6.3 Manual: Install URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/InstallURPIntoAProject.html) -- установка URP
- [Unity 6.3 Manual: Set up 2D Renderer](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/Setup.html) -- настройка 2D Renderer Asset
- [Unity Manual: Render Pipeline Converter](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/features/rp-converter.html) -- конвертация материалов
- [Unity 6.3 Manual: Particles Unlit shader](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/particles-unlit-shader.html) -- URP Particles шейдеры
- [Unity Manual: URP Introduction](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/urp-introduction.html) -- обзор URP

### Secondary (MEDIUM confidence)
- [URP 17.5.0 Changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.5/changelog/CHANGELOG.html) -- версия URP для Unity 6.3
- [Unity: How to move from Built-in to URP](https://unity.com/resources/how-to-move-from-built-in-to-urp) -- руководство по миграции
- [Converting Built-in to URP in Unity 6 (PreFure Wiki)](https://www.prefure.com/docs/technology/Unity/unity6_built_in_to_urp) -- пошаговое руководство

### Tertiary (LOW confidence)
- Точная версия URP 17.5.x в Unity 6.3 Registry -- определится при установке

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- URP 17.x для Unity 6 хорошо задокументирован, единственный вариант для 2D
- Architecture: HIGH -- стандартная процедура миграции, проект без кастомных шейдеров
- Pitfalls: HIGH -- типичные проблемы миграции хорошо известны и задокументированы
- Конвертация Default-Line/Default-Particle: MEDIUM -- может потребовать ручной работы

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (стабильная область, URP в LTS)
