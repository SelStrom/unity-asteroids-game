---
status: resolved
trigger: "Laser VFX remains on scene when player is killed while using laser"
created: 2026-04-03T00:00:00Z
updated: 2026-04-03T12:12:00Z
---

## Current Focus

hypothesis: CONFIRMED — ActionScheduler.ResetSchedule() in Stop() wipes the scheduled laser VFX cleanup action
test: Traced death path and laser VFX lifecycle
expecting: Missing cleanup for laser LineRenderer on ship death
next_action: Report root cause

## Symptoms

expected: When ship is destroyed, all visual effects including active laser should be cleaned up
actual: Laser visual effect (LineRenderer) persists on scene after ship death
errors: N/A (visual bug, no error)
reproduction: Fire laser, get killed while laser beam is still visible
started: Since original implementation — affects both MonoBehaviour and ECS paths

## Eliminated

## Evidence

- timestamp: 2026-04-03T00:01:00Z
  checked: Game.OnShipCollided (line 276-280)
  found: Calls Kill(_shipModel) then Stop(). Stop() calls ActionScheduler.ResetSchedule()
  implication: ResetSchedule clears ALL scheduled actions including pending laser VFX cleanup

- timestamp: 2026-04-03T00:02:00Z
  checked: Laser VFX creation in OnUserLaserShooting (line 356-364) and ProcessShootEvents (line 196-227)
  found: Both paths create LineRenderer via ViewFactory.Get, then schedule Release via ActionScheduler.ScheduleAction with BeamEffectLifetimeSec delay
  implication: Laser VFX cleanup depends entirely on ActionScheduler — no other cleanup path exists

- timestamp: 2026-04-03T00:03:00Z
  checked: ActionScheduler.ResetSchedule (line 62-66)
  found: Unconditionally clears _scheduledEntries list and resets _nextUpdateDuration
  implication: All pending actions are lost, including laser VFX Release

- timestamp: 2026-04-03T00:04:00Z
  checked: Model.CleanUp and EntitiesCatalog.Release
  found: CleanUp releases all entities (ship, asteroids, UFOs, bullets) but laser VFX LineRenderer is NOT an entity — it's a standalone pooled GameObject
  implication: Neither entity cleanup nor catalog cleanup touches orphaned laser VFX

- timestamp: 2026-04-03T00:05:00Z
  checked: ViewFactory pool mechanism
  found: Laser LineRenderer is obtained via ViewFactory.Get and only returned via ViewFactory.Release. No tracking of "active VFX" outside ActionScheduler
  implication: Once the scheduled Release action is lost, there is zero reference to the active laser LineRenderer

## Resolution

root_cause: |
  When the ship is killed, Game.OnShipCollided calls Kill() then Stop().
  Stop() calls ActionScheduler.ResetSchedule() which clears ALL scheduled actions.

  The laser VFX (LineRenderer GameObject) is cleaned up via a delayed action:
    ActionScheduler.ScheduleAction(() => ViewFactory.Release(effect.gameObject), BeamEffectLifetimeSec)

  When ResetSchedule() wipes this pending action, the LineRenderer remains active in the scene
  with no remaining reference to clean it up. It is not tracked as an entity, so entity cleanup
  does not touch it.

  This affects BOTH code paths:
  - MonoBehaviour path: Game.OnUserLaserShooting (line 363)
  - ECS path: Game.ProcessShootEvents (line 205-208)

fix:
verification:
files_changed: []
