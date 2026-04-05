---
phase: 14-config-visual-polish
plan: 01
subsystem: config, vfx
tags: [scriptableobject, particlesystem, vfx, ecs, rocket]

# Dependency graph
requires:
  - phase: 13-rocket-game-integration
    provides: "RocketData struct, EntityFactory.CreateRocket, RocketVisual, EntitiesCatalog.CreateRocket"
provides:
  - "RocketData ScriptableObject конфигурация со всеми параметрами ракеты"
  - "ScoreValue компонент на rocket entity для начисления очков"
  - "Trail ParticleSystem lifecycle в RocketVisual для пулинга"
  - "Взрыв VFX при уничтожении ракеты через VfxBlowPrefab"
affects: [14-02, gameplay-tuning]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ParticleSystem lifecycle для пулинга: Clear+Play в OnConnected, Stop+StopEmittingAndClear в OnDisable"

key-files:
  created: []
  modified:
    - Assets/Scripts/Configs/GameData.cs
    - Assets/Scripts/ECS/EntityFactory.cs
    - Assets/Scripts/Application/EntitiesCatalog.cs
    - Assets/Scripts/View/RocketVisual.cs
    - Assets/Scripts/Application/Application.cs

key-decisions:
  - "Переиспользование VfxBlowPrefab для взрыва ракеты (единообразно с астероидами и UFO)"
  - "ParticleSystem trail вместо TrailRenderer для корректной работы с ObjectPool"

patterns-established:
  - "Config struct pattern: все параметры ракеты в GameData.RocketData, читаются через _configs.Rocket"

requirements-completed: [CONF-01, VIS-02, VIS-04]

# Metrics
duration: 2min
completed: 2026-04-05
---

# Phase 14 Plan 01: Config & Visual Polish Summary

**Вынос hardcoded параметров ракеты в ScriptableObject, добавление ScoreValue на entity, trail ParticleSystem и взрыв VFX при уничтожении**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-05T21:53:21Z
- **Completed:** 2026-04-05T21:55:40Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- Расширен RocketData struct полями Speed, LifeTimeSec, TurnRateDegPerSec, MaxAmmo, ReloadDurationSec, Score
- Убраны все hardcoded значения из EntitiesCatalog (speed: 8f, lifeTime: 5f, turnRateDegPerSec: 180f, rocketMaxAmmo: 3, rocketReloadSec: 5f)
- EntityFactory.CreateRocket добавляет ScoreValue компонент для начисления очков
- RocketVisual управляет trail ParticleSystem lifecycle для корректного пулинга
- Application.OnDeadEntity воспроизводит взрыв VFX при уничтожении ракеты

## Task Commits

Each task was committed atomically:

1. **Task 1: Расширить RocketData, обновить EntityFactory и EntitiesCatalog** - `cd7abc6` (feat)
2. **Task 2: Trail ParticleSystem в RocketVisual и взрыв VFX в OnDeadEntity** - `cbe9a23` (feat)

## Files Created/Modified
- `Assets/Scripts/Configs/GameData.cs` - Расширен RocketData struct полями конфигурации
- `Assets/Scripts/ECS/EntityFactory.cs` - Добавлен параметр score и ScoreValue компонент в CreateRocket
- `Assets/Scripts/Application/EntitiesCatalog.cs` - Все параметры ракеты читаются из _configs.Rocket
- `Assets/Scripts/View/RocketVisual.cs` - Добавлен trail ParticleSystem с lifecycle для пулинга
- `Assets/Scripts/Application/Application.cs` - Добавлена ветка EntityType.Rocket в OnDeadEntity

## Decisions Made
- Переиспользование существующего VfxBlowPrefab для взрыва ракеты -- единообразно с астероидами, UFO и кораблём
- ParticleSystem trail вместо TrailRenderer -- лучше интегрируется с ObjectPool, поддерживает Stop/Clear

## Deviations from Plan

None - план выполнен точно как написан.

## Issues Encountered
None

## User Setup Required
None - внешняя конфигурация не требуется.

## Next Phase Readiness
- Все C#-файлы обновлены, код готов к компиляции
- Значения по умолчанию в GameData.asset нужно настроить в инспекторе Unity (Speed=8, LifeTimeSec=5, TurnRateDegPerSec=180, MaxAmmo=3, ReloadDurationSec=5, Score=50)
- Trail ParticleSystem нужно добавить как дочерний объект на Rocket префаб и привязать в инспекторе

## Self-Check: PASSED

All 5 modified files exist. Both task commits (cd7abc6, cbe9a23) verified. No stubs found.

---
*Phase: 14-config-visual-polish*
*Completed: 2026-04-05*
