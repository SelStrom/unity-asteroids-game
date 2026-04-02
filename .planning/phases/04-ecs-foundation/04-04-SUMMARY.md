---
phase: 04-ecs-foundation
plan: 04
subsystem: ecs
tags: [dots, entities, ai-systems, collision, unity-ecs]

# Dependency graph
requires:
  - phase: 04-ecs-foundation/01
    provides: "ECS components (MoveData, GunData, ShootToData, MoveToData, ShipPositionData, ScoreData, ScoreValue, CollisionEventData, DeadTag, tags), test fixture"
provides:
  - "EcsShootToSystem -- AI predictive aiming for UFOs"
  - "EcsMoveToSystem -- AI pursuit movement for small UFOs"
  - "EcsCollisionHandlerSystem -- collision processing with score tracking"
affects: [04-ecs-foundation, 05-dots-hybrid]

# Tech tracking
tech-stack:
  added: []
  patterns: ["ISystem query with RefRW/RefRO", "singleton pattern via GetSingleton", "collision event buffer processing"]

key-files:
  created:
    - Assets/Scripts/ECS/Systems/EcsShootToSystem.cs
    - Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs
    - Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs
    - Assets/Tests/EditMode/ECS/ShootToSystemTests.cs
    - Assets/Tests/EditMode/ECS/MoveToSystemTests.cs
    - Assets/Tests/EditMode/ECS/CollisionHandlerTests.cs
  modified: []

key-decisions:
  - "Hardcoded bullet speed 20f in ShootToSystem 1:1 with original (deferred QUAL-01)"
  - "Division by zero bugs preserved 1:1 from original code (deferred QUAL-01)"

patterns-established:
  - "AI system pattern: GetSingleton<ShipPositionData> for target data access"
  - "Collision handler: DynamicBuffer<CollisionEventData> iteration with tag-based entity classification"

requirements-completed: [ECS-09, ECS-10, ECS-11, TST-07, TST-08, TST-09]

# Metrics
duration: 2min
completed: 2026-04-02
---

# Phase 04 Plan 04: AI Systems and Collision Handler Summary

**EcsShootToSystem/EcsMoveToSystem for UFO AI (predictive aiming + pursuit) and EcsCollisionHandlerSystem with ScoreValue-based scoring, all via ShipPositionData singleton**

## Performance

- **Duration:** 2 min
- **Started:** 2026-04-02T21:52:11Z
- **Completed:** 2026-04-02T21:54:25Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- EcsShootToSystem with predictive aiming formula ported 1:1 from MonoBehaviour ShootToSystem
- EcsMoveToSystem with cooldown-based direction recalculation ported 1:1 from MonoBehaviour MoveToSystem
- EcsCollisionHandlerSystem processing CollisionEventData buffer with tag-based entity classification
- 12 EditMode tests covering all AI and collision scenarios

## Task Commits

Each task was committed atomically:

1. **Task 1: AI-systems (ShootTo, MoveTo) and tests**
   - `6990e85` (test) -- failing tests for ShootTo and MoveTo
   - `e50deda` (feat) -- implement EcsShootToSystem and EcsMoveToSystem
2. **Task 2: CollisionHandler and tests**
   - `d9ef532` (test) -- failing tests for EcsCollisionHandlerSystem
   - `8571e53` (feat) -- implement EcsCollisionHandlerSystem

_Note: TDD tasks have two commits each (test then feat)_

## Files Created/Modified
- `Assets/Scripts/ECS/Systems/EcsShootToSystem.cs` -- AI predictive aiming via ShipPositionData singleton
- `Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs` -- AI pursuit with cooldown timer via ShipPositionData
- `Assets/Scripts/ECS/Systems/EcsCollisionHandlerSystem.cs` -- collision processing, DeadTag marking, ScoreData update
- `Assets/Tests/EditMode/ECS/ShootToSystemTests.cs` -- 3 tests for ShootTo system
- `Assets/Tests/EditMode/ECS/MoveToSystemTests.cs` -- 3 tests for MoveTo system
- `Assets/Tests/EditMode/ECS/CollisionHandlerTests.cs` -- 6 tests for collision handler

## Decisions Made
- Hardcoded bullet speed 20f preserved 1:1 from original ShootToSystem (deferred to QUAL-01)
- Division by zero edge cases preserved 1:1 from original code (deferred to QUAL-01)
- CollisionHandler uses EntityManager.HasComponent for tag-based entity classification rather than archetypes

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Known Stubs
None - all systems fully implemented with complete logic.

## Next Phase Readiness
- All 3 AI/collision systems ready for integration
- System ordering chain established: ShootTo -> MoveTo -> CollisionHandler
- CollisionEventData buffer pattern ready for physics integration in Phase 05

---
*Phase: 04-ecs-foundation*
*Completed: 2026-04-02*
