---
phase: 04-ecs-foundation
plan: 01
subsystem: ecs
tags: [unity-entities, dots, ecs-components, icomponentdata]

# Dependency graph
requires:
  - phase: 03-urp-migration
    provides: Unity 6.3 + URP stable base for DOTS integration
provides:
  - com.unity.entities 1.4.5 package installed
  - 13 IComponentData structs (MoveData, ThrustData, RotateData, GunData, LaserData, ShootToData, MoveToData, LifeTimeData, AgeData, GameAreaData, ShipPositionData, ScoreData, ScoreValue)
  - 1 IBufferElementData (CollisionEventData)
  - 8 tag components (ShipTag, AsteroidTag, BulletTag, UfoTag, UfoBigTag, PlayerBulletTag, EnemyBulletTag, DeadTag)
  - AsteroidsECS assembly definition
  - EcsEditModeTests assembly definition
  - AsteroidsEcsTestFixture with entity creation helpers
  - 18 component tests
affects: [04-02, 04-03, 04-04]

# Tech tracking
tech-stack:
  added: [com.unity.entities 1.4.5]
  patterns: [IComponentData structs with float2, zero-size tag components, singleton pattern for shared state]

key-files:
  created:
    - Assets/Scripts/ECS/AsteroidsECS.asmdef
    - Assets/Scripts/ECS/Components/MoveData.cs
    - Assets/Scripts/ECS/Components/ScoreData.cs
    - Assets/Scripts/ECS/Components/CollisionEventData.cs
    - Assets/Scripts/ECS/Components/Tags/DeadTag.cs
    - Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef
    - Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs
    - Assets/Tests/EditMode/ECS/ComponentTests.cs
  modified:
    - Packages/manifest.json

key-decisions:
  - "Fallback test fixture (manual World creation) instead of ECSTestsFixture -- ECSTestsFixture availability uncertain without Unity compilation"
  - "CreateAndGetSystem uses ISystem (unmanaged) pattern per Unity Entities 1.4 best practices"
  - "ScoreData (singleton) and ScoreValue (per-entity) in single file ScoreData.cs per plan"

patterns-established:
  - "ECS components use float2 instead of Vector2, plain fields instead of ObservableValue"
  - "Tag components are zero-size structs implementing IComponentData"
  - "Singletons (GameAreaData, ShipPositionData, ScoreData) as regular IComponentData on dedicated entities"
  - "AsteroidsEcsTestFixture as base class for all ECS tests with entity factory helpers"

requirements-completed: [ECS-01, ECS-02, TST-01]

# Metrics
duration: 4min
completed: 2026-04-02
---

# Phase 04 Plan 01: ECS Components and Test Infrastructure Summary

**Unity Entities 1.4.5 installed with 22 IComponentData structs (13 data + 1 buffer + 8 tags) and 18 EditMode component tests via custom test fixture**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-02T21:43:30Z
- **Completed:** 2026-04-02T21:47:30Z
- **Tasks:** 3
- **Files modified:** 26

## Accomplishments
- Installed com.unity.entities 1.4.5 with testables configuration
- Created all 21 component files (13 data + 1 buffer + 8 tags) in SelStrom.Asteroids.ECS namespace
- Built AsteroidsEcsTestFixture with 9 entity creation helpers including ScoreValue for scored entities
- 18 component tests covering defaults, singletons, buffers, tags, and entity composition

## Task Commits

Each task was committed atomically:

1. **Task 1: Install com.unity.entities and create assembly definitions** - `3c72a46` (chore)
2. **Task 2: Create all IComponentData and tag components** - `7257f33` (feat)
3. **Task 3: Test fixture and EditMode component tests** - `9a99c1b` (test)

## Files Created/Modified
- `Packages/manifest.json` - Added com.unity.entities 1.4.5 and testables section
- `Assets/Scripts/ECS/AsteroidsECS.asmdef` - Assembly definition for ECS code
- `Assets/Scripts/ECS/Components/MoveData.cs` - Movement component (Position, Speed, Direction)
- `Assets/Scripts/ECS/Components/ThrustData.cs` - Thrust component with MinSpeed const
- `Assets/Scripts/ECS/Components/RotateData.cs` - Rotation component with DegreePerSecond const
- `Assets/Scripts/ECS/Components/GunData.cs` - Gun weapon component
- `Assets/Scripts/ECS/Components/LaserData.cs` - Laser weapon component
- `Assets/Scripts/ECS/Components/ShootToData.cs` - AI targeting timing
- `Assets/Scripts/ECS/Components/MoveToData.cs` - AI movement timing
- `Assets/Scripts/ECS/Components/LifeTimeData.cs` - Entity lifetime
- `Assets/Scripts/ECS/Components/AgeData.cs` - Asteroid age/size
- `Assets/Scripts/ECS/Components/GameAreaData.cs` - Game area singleton
- `Assets/Scripts/ECS/Components/ShipPositionData.cs` - Ship position singleton
- `Assets/Scripts/ECS/Components/ScoreData.cs` - Score singleton + ScoreValue per-entity
- `Assets/Scripts/ECS/Components/CollisionEventData.cs` - Collision event buffer
- `Assets/Scripts/ECS/Components/Tags/*.cs` - 8 tag components
- `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef` - Test assembly definition
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` - Base test fixture with helpers
- `Assets/Tests/EditMode/ECS/ComponentTests.cs` - 18 component tests

## Decisions Made
- Used fallback test fixture (manual World creation) instead of ECSTestsFixture -- compilation environment unavailable to verify ECSTestsFixture availability
- CreateAndGetSystem uses ISystem (unmanaged) pattern per Unity Entities 1.4 best practices
- ScoreData (singleton) and ScoreValue (per-entity) combined in single file per plan specification

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All 22 component types ready for EntityFactory (Plan 02) and system implementations (Plan 03)
- ScoreValue and CollisionEventData available for CollisionHandler (Plan 04)
- AsteroidsEcsTestFixture reusable across all subsequent ECS test plans
- Test infrastructure (asmdef, fixture) established for TDD workflow

## Self-Check: PASSED

All 8 key files verified present. All 3 task commits verified in git log.

---
*Phase: 04-ecs-foundation*
*Completed: 2026-04-02*
