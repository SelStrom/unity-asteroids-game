---
phase: 5
slug: bridge-layer-integration
status: draft
nyquist_compliant: false
wave_0_complete: false
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

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 05-01-01 | 01 | 1 | BRG-01 | unit (EditMode) | Test Runner EditMode | ❌ W0 | ⬜ pending |
| 05-01-02 | 01 | 1 | BRG-02 | unit (EditMode) | Test Runner EditMode | ❌ W0 | ⬜ pending |
| 05-02-01 | 02 | 1 | BRG-03 | unit (EditMode) | Test Runner EditMode | ❌ W0 | ⬜ pending |
| 05-02-02 | 02 | 1 | BRG-04 | unit (EditMode) | Test Runner EditMode | ❌ W0 | ⬜ pending |
| 05-03-01 | 03 | 2 | BRG-05 | unit (EditMode) | Test Runner EditMode | ❌ W0 | ⬜ pending |
| 05-03-02 | 03 | 2 | BRG-06 | PlayMode + manual | Test Runner PlayMode | ❌ W0 | ⬜ pending |
| 05-03-03 | 03 | 2 | TST-10 | unit (EditMode) | Test Runner EditMode | ❌ W0 | ⬜ pending |
| 05-03-04 | 03 | 2 | TST-12 | integration (PlayMode) | Test Runner PlayMode | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/ECS/GameObjectSyncSystemTests.cs` — stubs for BRG-01, BRG-02
- [ ] `Assets/Tests/EditMode/ECS/CollisionBridgeTests.cs` — stubs for BRG-03
- [ ] `Assets/Tests/EditMode/ECS/ObservableBridgeSystemTests.cs` — stubs for BRG-04
- [ ] `Assets/Tests/EditMode/ECS/DeadEntityCleanupSystemTests.cs` — stubs for BRG-05
- [ ] `Assets/Tests/PlayMode/GameplayCycleTests.cs` — stubs for TST-12
- [ ] `PlayModeTests.asmdef` — add references to AsteroidsECS, Unity.Entities
- [ ] `EcsEditModeTests.asmdef` — add references to Asteroids, Shtl.Mvvm for bridge tests

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual gameplay 1:1 | BRG-06 | Visual comparison requires human eye | Play full game, verify: ship movement, bullets, laser, asteroid splitting, UFO AI, HUD values, score display, leaderboard |
| Thrust sprite toggle | BRG-04 | Sprite visual verification | Hold W, verify thrust sprite shows; release W, verify default sprite |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
