---
phase: 01-dev-tooling-shtl-mvvm-fix
plan: 01
subsystem: infra
tags: [shtl-mvvm, unity6, textmeshpro, ugui, upm, conditional-compilation]

# Dependency graph
requires: []
provides:
  - "Git tag v1.1.0 shtl-mvvm с фиксом зависимости textmeshpro -> ugui"
  - "Условная компиляция FindObjectsByType для Unity 2023.1+"
  - "Обратная совместимость с Unity 2022.3+"
affects: [01-02, 01-03, 02-unity6-upgrade]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Условная компиляция через UNITY_2023_1_OR_NEWER для deprecated API"

key-files:
  created: []
  modified:
    - "~/work/projects/shtl-mvvm/package.json"
    - "~/work/projects/shtl-mvvm/Runtime/DevWidget.cs"

key-decisions:
  - "Заменить com.unity.textmeshpro на com.unity.ugui в package.json -- ugui тянет TMP транзитивно в Unity 6"
  - "Сохранить Unity.TextMeshPro ссылку в asmdef -- assembly forwarding работает в Unity 6"
  - "Версия 1.1.0 для семантического обозначения breaking change в зависимостях"

patterns-established:
  - "Условная компиляция: #if UNITY_2023_1_OR_NEWER для deprecated Unity API"

requirements-completed: [MVVM-01, MVVM-02, MVVM-03, MVVM-04, MVVM-05]

# Metrics
duration: 2min
completed: 2026-04-02
---

# Phase 01 Plan 01: shtl-mvvm fix Summary

**Замена зависимости textmeshpro на ugui и фикс deprecated FindObjectsOfType в shtl-mvvm v1.1.0**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-02T17:16:58Z
- **Completed:** 2026-04-02T17:18:24Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- Заменена зависимость com.unity.textmeshpro на com.unity.ugui в package.json shtl-mvvm
- Добавлена условная компиляция FindObjectsByType для Unity 2023.1+ с сохранением FindObjectsOfType для старых версий
- Опубликован git tag v1.1.0 в github.com/SelStrom/shtl-mvvm

## Task Commits

Each task was committed atomically:

1. **Task 1+2: Применить фикс, закоммитить, создать тег и запушить** - `5802404` (fix) -- в репозитории shtl-mvvm
2. **Task 3: Проверить публикацию тега v1.1.0** - auto-approved (checkpoint:human-verify)

**Note:** Коммиты сделаны в отдельном репозитории ~/work/projects/shtl-mvvm, не в asteroids.

## Files Created/Modified
- `~/work/projects/shtl-mvvm/package.json` -- Замена textmeshpro -> ugui, version 0.1.0 -> 1.1.0
- `~/work/projects/shtl-mvvm/Runtime/DevWidget.cs` -- Условная компиляция FindObjectsByType/FindObjectsOfType

## Decisions Made
- Заменить com.unity.textmeshpro на com.unity.ugui -- ugui является базовым пакетом, TMP тянется транзитивно
- Сохранить Unity.TextMeshPro в asmdef -- assembly forwarding в Unity 6 гарантирует работу
- Bump version до 1.1.0 -- семантический SemVer для смены зависимостей

## Deviations from Plan

None -- план выполнен точно как написано.

## Issues Encountered

None.

## Known Stubs

None -- все изменения полноценны, заглушек нет.

## Next Phase Readiness
- Tag v1.1.0 опубликован и доступен для обновления в Packages/manifest.json проекта asteroids
- Следующий шаг: Plan 02 обновит ссылку на shtl-mvvm в asteroids до v1.1.0

## Self-Check: PASSED

- FOUND: 01-01-SUMMARY.md
- FOUND: commit 5802404 in shtl-mvvm repo
- FOUND: tag v1.1.0 in shtl-mvvm repo

---
*Phase: 01-dev-tooling-shtl-mvvm-fix*
*Completed: 2026-04-02*
