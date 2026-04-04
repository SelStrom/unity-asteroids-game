---
phase: 4
slug: ecs-foundation
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-02
audited: 2026-04-04
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit) 1.1.33 |
| **Config file** | Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef |
| **Quick run command** | `Unity -runTests -testPlatform EditMode -testFilter ECS` |
| **Full suite command** | `Unity -runTests -testPlatform EditMode` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run EditMode ECS tests via Unity-MCP
- **After every plan wave:** Run full EditMode suite
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 04-01-01 | 01 | 1 | ECS-01 | integration | `grep -q "com.unity.entities" Packages/manifest.json` | ✅ | ✅ green |
| 04-01-02 | 01 | 1 | ECS-02, TST-01 | unit | `Unity -runTests -testPlatform EditMode -testFilter ComponentTests` | ✅ | ✅ green |
| 04-02-01 | 02 | 1 | ECS-03 | unit | `Unity -runTests -testPlatform EditMode -testFilter EntityFactoryTests` | ✅ | ✅ green |
| 04-02-02 | 02 | 1 | ECS-04, TST-02 | unit | `Unity -runTests -testPlatform EditMode -testFilter ThrustSystemTests` | ✅ | ✅ green |
| 04-02-03 | 02 | 1 | ECS-05, TST-04 | unit | `Unity -runTests -testPlatform EditMode -testFilter RotateSystemTests` | ✅ | ✅ green |
| 04-02-04 | 02 | 1 | ECS-06, TST-03 | unit | `Unity -runTests -testPlatform EditMode -testFilter MoveSystemTests` | ✅ | ✅ green |
| 04-03-01 | 03 | 2 | ECS-07, TST-05 | unit | `Unity -runTests -testPlatform EditMode -testFilter GunSystemTests` | ✅ | ✅ green |
| 04-03-02 | 03 | 2 | ECS-08, TST-06 | unit | `Unity -runTests -testPlatform EditMode -testFilter LaserSystemTests` | ✅ | ✅ green |
| 04-04-01 | 04 | 2 | ECS-09, TST-07 | unit | `Unity -runTests -testPlatform EditMode -testFilter ShootToSystemTests` | ✅ | ✅ green |
| 04-04-02 | 04 | 2 | ECS-10, TST-08 | unit | `Unity -runTests -testPlatform EditMode -testFilter MoveToSystemTests` | ✅ | ✅ green |
| 04-04-03 | 04 | 2 | ECS-11, TST-09 | unit | `Unity -runTests -testPlatform EditMode -testFilter CollisionHandlerTests` | ✅ | ✅ green |

*Status: ⬜ pending / ✅ green / ❌ red / ⚠ flaky*

---

## Test Coverage Summary

| Test File | [Test] Count | Requirements |
|-----------|-------------|--------------|
| ComponentTests.cs | 18 | ECS-02, TST-01 |
| EntityFactoryTests.cs | 10 | ECS-03 |
| RotateSystemTests.cs | 3 | ECS-05, TST-04 |
| ThrustSystemTests.cs | 4 | ECS-04, TST-02 |
| MoveSystemTests.cs | 4 | ECS-06, TST-03 |
| GunSystemTests.cs | 6 | ECS-07, TST-05 |
| EcsGunSystemTests.cs | 7 | ECS-07, TST-05 |
| LaserSystemTests.cs | 7 | ECS-08, TST-06 |
| EcsLaserSystemTests.cs | 6 | ECS-08, TST-06 |
| ShootToSystemTests.cs | 3 | ECS-09, TST-07 |
| MoveToSystemTests.cs | 3 | ECS-10, TST-08 |
| CollisionHandlerTests.cs | 10 | ECS-11, TST-09 |
| **Total** | **81** | |

---

## Wave 0 Requirements

- [x] `Assets/Scripts/ECS/AsteroidsECS.asmdef` -- ECS assembly definition with Unity.Entities dependency
- [x] `Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef` -- test assembly referencing AsteroidsECS
- [x] `Packages/manifest.json` -- com.unity.entities 1.4.5 added, testables entry present

*Existing NUnit infrastructure covers test execution.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Burst compilation succeeds | ECS-04/05/06 | Burst errors only visible in Editor console | Check Burst Inspector window for compilation status |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** complete (audited 2026-04-04)
