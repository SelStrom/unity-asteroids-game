---
phase: 9
slug: ecs-tech-debt-cleanup
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-04
---

# Phase 9 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit (Unity Test Framework 1.1.33) |
| **Config file** | Assets/Tests/EditMode/EditMode.asmdef |
| **Quick run command** | `mcp__ai-game-developer__tests-run --testMode EditMode` |
| **Full suite command** | `mcp__ai-game-developer__tests-run --testMode EditMode` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `mcp__ai-game-developer__tests-run --testMode EditMode`
- **After every plan wave:** Run `mcp__ai-game-developer__tests-run --testMode EditMode`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 09-01-01 | 01 | 1 | TD-01 | grep | `grep 'UpdateAfter\|UpdateBefore' Assets/Scripts/ECS/Systems/EcsGunSystem.cs` | N/A | pending |
| 09-01-02 | 01 | 1 | TD-01 | grep | `grep 'UpdateAfter\|UpdateBefore' Assets/Scripts/ECS/Systems/EcsLaserSystem.cs` | N/A | pending |
| 09-01-03 | 01 | 1 | TD-02 | grep | `grep 'UpdateAfter.*EcsShipPositionUpdateSystem' Assets/Scripts/ECS/Systems/EcsShootToSystem.cs Assets/Scripts/ECS/Systems/EcsMoveToSystem.cs` | N/A | pending |
| 09-01-04 | 01 | 1 | TD-03 | grep | `grep -c 'ReadyRemaining\|Every' Assets/Scripts/ECS/Components/ShootToData.cs` | N/A | pending |
| 09-02-01 | 02 | 1 | TD-04 | grep | `grep -c 'Position.*ReactiveValue' Assets/Scripts/View/AsteroidVisual.cs Assets/Scripts/View/BulletVisual.cs Assets/Scripts/View/UfoVisual.cs` | N/A | pending |
| 09-02-02 | 02 | 1 | TD-05 | grep | grep for double-write pattern in ShipVisual/ObservableBridgeSystem | N/A | pending |
| 09-03-01 | 03 | 1 | TD-06 | git | `git status Assets/Tests/` | N/A | pending |

*Status: pending / green / red / flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test files needed — verification is via grep and code inspection.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Game still plays correctly after ordering changes | TD-01, TD-02 | Ordering affects runtime behavior timing | Play game in Editor, verify gun/laser fire, UFO AI works |
| Ship movement smooth without double-write | TD-05 | Visual smoothness hard to automate | Play game, observe ship movement and rotation |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
