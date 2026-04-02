---
phase: 02-unity-6-3-upgrade
plan: 03
subsystem: testing, infra
tags: [asmdef, urp, unity-6-3, timedata, compilation-fix]

requires:
  - phase: 02-unity-6-3-upgrade/02-01
    provides: "Unity 6.3 upgrade and base package migration"
  - phase: 02-unity-6-3-upgrade/02-02
    provides: "URP setup and test infrastructure"
provides:
  - "Zero compilation errors in Unity 6.3 Editor"
  - "URP assembly reference for EditMode tests"
  - "Deduplicated EcsEditModeTests.asmdef references"
  - "Fully qualified Unity.Core.TimeData in ECS tests"
affects: [03-urp-migration, 04-ecs-foundation, 05-bridge-layer]

tech-stack:
  added: []
  patterns: ["Fully qualified Unity.Core.TimeData to avoid namespace conflicts with Unity.Entities"]

key-files:
  created: []
  modified:
    - Assets/Tests/EditMode/EditModeTests.asmdef
    - Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef
    - Assets/Tests/EditMode/ECS/MoveSystemTests.cs
    - Assets/Tests/EditMode/ECS/ThrustSystemTests.cs
    - Assets/Tests/EditMode/ECS/RotateSystemTests.cs
    - Packages/manifest.json

key-decisions:
  - "Inline Unity.Core.TimeData qualification instead of using-directive to prevent namespace conflicts"

patterns-established:
  - "Unity.Core.TimeData: always use fully qualified name in ECS test files"

requirements-completed: [UPG-01, UPG-02]

duration: 1min
completed: 2026-04-02
---

# Phase 02 Plan 03: Gap Closure Summary

**Fix 3 compilation blockers: URP assembly ref in EditModeTests, duplicate refs in EcsEditModeTests, unqualified TimeData in 3 ECS test files; remove deprecated vscode package**

## Performance

- **Duration:** 1 min
- **Started:** 2026-04-02T23:25:24Z
- **Completed:** 2026-04-02T23:26:40Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- EditModeTests.asmdef now references URP Runtime assembly (GUID:15fc0a57446b3144c949da3e2b9737a9), unblocking UrpSetupTests.cs and UrpPostProcessingTests.cs compilation
- EcsEditModeTests.asmdef cleaned from duplicate Asteroids and Shtl.Mvvm references
- All 11 occurrences of bare `TimeData` in 3 test files qualified as `Unity.Core.TimeData`
- Deprecated com.unity.ide.vscode removed from manifest.json

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix asmdef files (URP ref + duplicates)** - `78c65b1` (fix)
2. **Task 2: Qualify TimeData and remove deprecated package** - `bb635e9` (fix)

## Files Created/Modified
- `Assets/Tests/EditMode/EditModeTests.asmdef` - Added URP Runtime GUID reference
- `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef` - Removed duplicate Asteroids and Shtl.Mvvm entries
- `Assets/Tests/EditMode/ECS/MoveSystemTests.cs` - 4x TimeData -> Unity.Core.TimeData
- `Assets/Tests/EditMode/ECS/ThrustSystemTests.cs` - 4x TimeData -> Unity.Core.TimeData
- `Assets/Tests/EditMode/ECS/RotateSystemTests.cs` - 3x TimeData -> Unity.Core.TimeData
- `Packages/manifest.json` - Removed com.unity.ide.vscode 1.2.5

## Decisions Made
- Inline `Unity.Core.TimeData` qualification (not `using Unity.Core`) to prevent namespace conflicts with `Unity.Entities.TimeData`

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All 3 compilation bugs from UAT resolved
- Project should compile cleanly in Unity 6.3 Editor (0 errors)
- Ready for user to verify in Unity Editor Console

---
*Phase: 02-unity-6-3-upgrade*
*Completed: 2026-04-02*
