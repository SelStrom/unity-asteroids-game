---
phase: 10
slug: ecs-core
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-05
---

# Phase 10 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.1.33 (NUnit) |
| **Config file** | Assets/Tests/EditMode/EditModeTests.asmdef |
| **Quick run command** | Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All |
| **Full suite command** | Unity Editor -> Test Runner -> Run All (EditMode + PlayMode) |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run EditMode tests via Test Runner
- **After every plan wave:** Run full suite (EditMode + PlayMode)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 10-01-01 | 01 | 1 | ROCK-02 | — | N/A | unit | Test Runner -> RocketGuidanceSystemTests | ❌ W0 | ⬜ pending |
| 10-01-02 | 01 | 1 | ROCK-02 | — | N/A | unit | Test Runner -> RocketGuidanceSystemTests (turn rate) | ❌ W0 | ⬜ pending |
| 10-01-03 | 01 | 1 | ROCK-03 | — | N/A | unit | Test Runner -> RocketGuidanceSystemTests (retarget) | ❌ W0 | ⬜ pending |
| 10-01-04 | 01 | 1 | ROCK-04 | — | N/A | unit | Existing EcsLifeTimeSystem tests | ✅ | ⬜ pending |
| 10-01-05 | 01 | 1 | ROCK-05 | — | N/A | unit | Test Runner -> RocketAmmoSystemTests | ❌ W0 | ⬜ pending |
| 10-01-06 | 01 | 1 | ROCK-06 | — | N/A | unit | Test Runner -> RocketAmmoSystemTests (reload) | ❌ W0 | ⬜ pending |
| 10-01-07 | 01 | 1 | TEST-01 | — | N/A | meta | All above green | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/ECS/RocketGuidanceSystemTests.cs` — stubs for ROCK-02, ROCK-03
- [ ] `Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs` — stubs for ROCK-05, ROCK-06
- [ ] `Assets/Tests/EditMode/ECS/RocketComponentTests.cs` — stubs for component creation

*Existing infrastructure (AsteroidsEcsTestFixture) covers test setup.*

---

## Manual-Only Verifications

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
