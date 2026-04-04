---
phase: 02-unity-6-3-upgrade
plan: 01
subsystem: infra
tags: [unity6, textmeshpro, asmdef, guid, tmp-migration]

# Dependency graph
requires:
  - phase: 01-dev-tooling-shtl-mvvm-fix
    provides: shtl-mvvm v1.1.0 с зависимостью на com.unity.ugui вместо com.unity.textmeshpro
provides:
  - Чистый проект без локальных TMP-ассетов
  - Все asmdef используют GUID-ссылки на Unity.TextMeshPro
  - Подтверждено отсутствие deprecated API в Assets/Scripts/
affects: [02-unity-6-3-upgrade]

# Tech tracking
tech-stack:
  added: []
  patterns: [GUID-ссылки в asmdef вместо строковых имён]

key-files:
  created: []
  modified:
    - Assets/Tests/EditMode/EditModeTests.asmdef
    - Assets/Tests/PlayMode/PlayModeTests.asmdef
    - Assets/Editor/AsteroidsEditor.asmdef

key-decisions:
  - "GUID 6055be8ebefd69e48b49212b09b47b2f для Unity.TextMeshPro -- единый стандарт во всех asmdef"

patterns-established:
  - "GUID-ссылки: все asmdef должны ссылаться на TMP через GUID, не строковое имя"

requirements-completed: [UPG-01, UPG-02, UPG-03, UPG-04]

# Metrics
duration: 2min
completed: 2026-04-02
---

# Phase 02 Plan 01: TMP cleanup and asmdef GUID migration Summary

**Удалены локальные TMP-ассеты (73 файла), asmdef переведены на GUID-ссылки, deprecated API не обнаружены**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-02T19:06:33Z
- **Completed:** 2026-04-02T19:08:43Z
- **Tasks:** 2 (1 auto + 1 checkpoint auto-approved)
- **Files modified:** 73

## Accomplishments
- Удалена директория Assets/TextMesh Pro/ целиком (шрифты, шейдеры, спрайты, ресурсы -- 70 файлов)
- Три asmdef-файла переведены со строковой ссылки "Unity.TextMeshPro" на GUID:6055be8ebefd69e48b49212b09b47b2f
- Подтверждено отсутствие deprecated API (FindObjectsOfType, SendMessage и др.) в Assets/Scripts/
- Подтверждено отсутствие compiler warning pragmas в Assets/Scripts/

## Task Commits

Each task was committed atomically:

1. **Task 1: Удалить локальные TMP-ассеты и исправить asmdef-ссылки** - `f39de4a` (feat)
2. **Task 2: Верификация компиляции и TMP в Unity Editor** - auto-approved (checkpoint)

## Files Created/Modified
- `Assets/TextMesh Pro/` - Удалена целиком (70 файлов: шрифты, шейдеры, спрайты, ресурсы)
- `Assets/TextMesh Pro.meta` - Удалён
- `Assets/Tests/EditMode/EditModeTests.asmdef` - Заменена строковая ссылка TMP на GUID
- `Assets/Tests/PlayMode/PlayModeTests.asmdef` - Заменена строковая ссылка TMP на GUID
- `Assets/Editor/AsteroidsEditor.asmdef` - Заменена строковая ссылка TMP на GUID

## Decisions Made
- GUID 6055be8ebefd69e48b49212b09b47b2f используется как единый стандарт для ссылки на Unity.TextMeshPro во всех asmdef (совпадает с эталонным Asteroids.asmdef)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
Требуется ручное действие в Unity Editor:
- Открыть проект в Unity 6000.3.12f1
- Выполнить Window > TextMeshPro > Import TMP Essential Resources
- Проверить отсутствие ошибок компиляции и привязку шрифтов

## Next Phase Readiness
- Код готов к компиляции в Unity 6.3 после импорта TMP Essential Resources
- Следующий план (02-02) -- верификация геймплея и функциональная эквивалентность

## Self-Check: PASSED

- FOUND: EditModeTests.asmdef
- FOUND: PlayModeTests.asmdef
- FOUND: AsteroidsEditor.asmdef
- CONFIRMED: TMP dir deleted
- FOUND: commit f39de4a

---
*Phase: 02-unity-6-3-upgrade*
*Completed: 2026-04-02*
