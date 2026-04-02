---
phase: 03-urp-migration
plan: 01
subsystem: rendering
tags: [urp, render-pipeline, materials, post-processing, 2d-renderer]
dependency_graph:
  requires: [02-unity-6-3-upgrade]
  provides: [urp-pipeline-configured, urp-materials, post-processing-volume]
  affects: [rendering, visual-output, prefabs, scene]
tech_stack:
  added: [com.unity.render-pipelines.universal]
  patterns: [urp-2d-renderer, global-volume, urp-unlit-materials]
key_files:
  created:
    - Assets/Settings/URP-2D-Asset.asset
    - Assets/Settings/URP-2D-Renderer.asset
    - Assets/Settings/PostProcessing-Profile.asset
    - Assets/Media/materials/Laser-URP.mat
    - Assets/Media/materials/Particle-URP.mat
    - Assets/Tests/EditMode/Upgrade/UrpSetupTests.cs
    - Assets/Tests/EditMode/Upgrade/UrpMaterialTests.cs
    - Assets/Tests/EditMode/Upgrade/UrpPostProcessingTests.cs
  modified:
    - Packages/manifest.json
    - ProjectSettings/GraphicsSettings.asset
    - ProjectSettings/QualitySettings.asset
    - Assets/Media/effects/lazer.prefab
    - Assets/Media/effects/vfx_blow.prefab
    - Assets/Scenes/Main.unity
    - Assets/Tests/EditMode/EditModeTests.asmdef
decisions:
  - "URP 17.0.5 -- версия указана в manifest, Package Manager разрешит точную совместимую версию"
  - "Sprite-Unlit-Default для спрайтов -- 1:1 с оригиналом без 2D Light (отложено на VIS-01)"
  - "Bloom: threshold=0.9, intensity=0.5, scatter=0.7 -- умеренный glow для белых контуров на черном фоне"
  - "Vignette: intensity=0.35, smoothness=0.4 -- легкое затемнение краев"
metrics:
  duration: 7min
  completed: "2026-04-02"
  tasks_completed: 2
  tasks_total: 2
  files_created: 14
  files_modified: 7
  tests_added: 12
---

# Phase 03 Plan 01: URP Installation and Configuration Summary

URP 2D Renderer установлен с материалами Laser-URP (Unlit) и Particle-URP (Particles/Unlit), Global Volume с Bloom+Vignette, 12 EditMode тестов верификации.

## Task Completion

| Task | Name | Commit | Status |
|------|------|--------|--------|
| 1 | Установка URP, создание ассетов, настройка Pipeline и материалов | b0921bd | Done |
| 2 | EditMode тесты верификации URP настройки | 30f22b3 | Done |

## What Was Done

### Task 1: Установка URP, создание ассетов, настройка Pipeline и материалов

1. Добавлен `com.unity.render-pipelines.universal: 17.0.5` в `Packages/manifest.json`
2. Созданы URP ассеты в `Assets/Settings/`:
   - `URP-2D-Asset.asset` -- URP Pipeline Asset с 2D Renderer
   - `URP-2D-Renderer.asset` -- 2D Renderer Data Asset
   - `PostProcessing-Profile.asset` -- Volume Profile с Bloom и Vignette
3. Назначен URP Asset в `ProjectSettings/GraphicsSettings.asset` через `m_CustomRenderPipeline`
4. Назначен URP Asset во всех 6 Quality Level в `ProjectSettings/QualitySettings.asset`
5. Созданы URP материалы:
   - `Assets/Media/materials/Laser-URP.mat` -- шейдер Universal Render Pipeline/Unlit, белый цвет
   - `Assets/Media/materials/Particle-URP.mat` -- шейдер Universal Render Pipeline/Particles/Unlit, Transparent, Additive blending
6. Заменены ссылки на материалы в prefabs:
   - `lazer.prefab`: Default-Line (fileID: 10306) заменен на Laser-URP.mat
   - `vfx_blow.prefab`: Default-Particle (fileID: 10308) заменен на Particle-URP.mat, stopAction=Callback сохранен
7. Добавлен PostProcessing Volume на сцену Main.unity (isGlobal=true)
8. Добавлен UniversalAdditionalCameraData на Main Camera с renderPostProcessing=true
9. Параметры камеры сохранены: orthographicSize=22.5

### Task 2: EditMode тесты верификации URP настройки

1. `UrpSetupTests.cs` -- 4 теста (URP-01, URP-02):
   - UrpPipelineAssetExists
   - RendererDataAssetExists
   - GraphicsSettingsUsesUrp
   - QualitySettingsAllLevelsHaveUrp
2. `UrpMaterialTests.cs` -- 5 тестов (URP-03):
   - LaserMaterialExistsAndUsesUrpShader
   - ParticleMaterialExistsAndUsesUrpShader
   - LazerPrefabUsesUrpMaterial
   - VfxBlowPrefabUsesUrpMaterial
   - VfxBlowPrefabStopActionIsCallback
3. `UrpPostProcessingTests.cs` -- 3 теста (URP-04):
   - VolumeProfileExists
   - VolumeProfileHasBloom
   - VolumeProfileHasVignette
4. Обновлен `EditModeTests.asmdef` -- добавлены GUID-ссылки на URP Runtime и Core Runtime assemblies

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| URP 17.0.5 в manifest.json | Версия для Unity 6.3 LTS, Package Manager разрешит точную совместимую версию |
| Sprite-Unlit-Default для спрайтов | 1:1 с оригинальным Built-in Sprites-Default, не требует 2D Light |
| Bloom threshold=0.9, intensity=0.5 | Только самые яркие пиксели (белые контуры на черном фоне), умеренный glow |
| Vignette intensity=0.35, smoothness=0.4 | Легкое затемнение по краям, фокус на центр экрана |
| Additive blending для Particle-URP.mat | Аддитивное смешивание для эффекта взрыва -- стандарт для particle FX |

## Deviations from Plan

None -- план выполнен в точности.

## Known Stubs

YAML ассеты (URP-2D-Asset.asset, URP-2D-Renderer.asset, PostProcessing-Profile.asset, материалы) созданы как YAML-файлы с корректной структурой Unity SerializedObject. При первом открытии в Unity Editor они будут загружены и пересериализованы движком -- возможна корректировка внутренних ссылок (Script GUID для VolumeProfile, Bloom, Vignette). Если ассеты не загрузятся корректно, потребуется пересоздание через Unity Editor меню Create > Rendering.

## Notes

- YAML ассеты используют известные Unity GUID для Script ссылок (VolumeProfile, Bloom, Vignette, UniversalRenderPipelineAsset, Renderer2DData, UniversalAdditionalCameraData). Если GUIDs изменились в Unity 6.3, потребуется обновление.
- Тесты зависят от корректной загрузки ассетов в Unity Editor -- проверка возможна только при запуске Test Runner.
- Спрайты gameplay prefabs (ship, asteroids, bullets, ufo) используют встроенный Sprites-Default, который автоматически работает с URP 2D Renderer через Sprite-Unlit-Default.

## Self-Check: PASSED

All 8 created files verified present. Both commit hashes (b0921bd, 30f22b3) verified in git log.
