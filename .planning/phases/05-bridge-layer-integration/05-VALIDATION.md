---
phase: 5
slug: bridge-layer-integration
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-03
---

# Phase 5 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.1.33 (NUnit) |
| **Config file** | `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef`, `Assets/Tests/PlayMode/PlayModeTests.asmdef` |
| **Quick run command** | Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All |
| **Full suite command** | Unity Editor -> Window -> General -> Test Runner -> Run All (EditMode + PlayMode) |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run EditMode ECS tests
- **After every plan wave:** Run all EditMode + PlayMode tests
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Nyquist Compliance

All TDD tasks (`tdd="true"`) create test files inline as part of task execution — tests are written before implementation code per RED-GREEN-REFACTOR cycle. No separate Wave 0 stub plan is needed because:

1. Plan 01 Task 1 creates `EcsGunSystemTests.cs` and `EcsLaserSystemTests.cs` inline (TST-05, TST-06)
2. Plan 01 Task 2 creates `GameObjectSyncSystemTests.cs` inline
3. Plan 02 Task 1 creates `CollisionBridgeTests.cs` inline
4. Plan 02 Task 2 creates `ObservableBridgeSystemTests.cs` and `DeadEntityCleanupSystemTests.cs` inline
5. Plan 03 Task 3 creates `GameplayCycleTests.cs` inline (TST-12)

Each TDD task's `<behavior>` block defines test expectations before implementation. The `<verify>` block includes automated grep commands to confirm test file existence and test count.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------|-------------------|--------|
| 05-01-01 | 01 | 1 | BRG-01, ECS-07, ECS-08, TST-05, TST-06 | unit (EditMode, TDD inline) | grep test count | ⬜ pending |
| 05-01-02 | 01 | 1 | BRG-02 | unit (EditMode, TDD inline) | grep test count | ⬜ pending |
| 05-02-01 | 02 | 1 | BRG-03 | unit (EditMode, TDD inline) | grep test count | ⬜ pending |
| 05-02-02 | 02 | 1 | BRG-04, BRG-05, TST-10 | unit (EditMode, TDD inline) | grep test count | ⬜ pending |
| 05-03-01 | 03 | 2 | BRG-05 | integration | grep _useEcs | ⬜ pending |
| 05-03-02 | 03 | 2 | BRG-05, BRG-06 | integration | grep GunShootEvent | ⬜ pending |
| 05-03-03 | 03 | 2 | TST-12 | integration (PlayMode, TDD inline) | grep UnityTest count | ⬜ pending |
| 05-03-04 | 03 | 2 | BRG-06 | manual | human-verify | ⬜ pending |

*Status: ⬜ pending / ✅ green / ❌ red / ⚠️ flaky*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual gameplay 1:1 | BRG-06 | Visual comparison requires human eye | Play full game, verify: ship movement, bullets, laser, asteroid splitting, UFO AI, HUD values, score display, leaderboard |
| Thrust sprite toggle | BRG-04 | Sprite visual verification | Hold W, verify thrust sprite shows; release W, verify default sprite |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (TDD tasks create tests inline)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] No Wave 0 needed (TDD inline pattern)
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved
