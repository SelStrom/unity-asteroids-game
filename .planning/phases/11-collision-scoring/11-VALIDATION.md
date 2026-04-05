---
phase: 11
slug: collision-scoring
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-05
---

# Phase 11 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit (Unity Test Framework 1.1.33) |
| **Config file** | `Assets/Tests/EditMode/EditMode.asmdef` |
| **Quick run command** | `mcp__ai-game-developer__tests-run --testMode EditMode --testFilter CollisionHandler` |
| **Full suite command** | `mcp__ai-game-developer__tests-run --testMode EditMode` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick command (CollisionHandler filter)
- **After every plan wave:** Run full EditMode suite
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 11-01-01 | 01 | 1 | COLL-01 | — | N/A | unit | `tests-run --testFilter RocketHitsAsteroid` | ❌ W0 | ⬜ pending |
| 11-01-02 | 01 | 1 | COLL-02 | — | N/A | unit | `tests-run --testFilter RocketHitsUfo` | ❌ W0 | ⬜ pending |
| 11-01-03 | 01 | 1 | COLL-03 | — | N/A | unit | `tests-run --testFilter RocketDestroyed` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*
- `Assets/Tests/EditMode/ECS/CollisionHandlerTests.cs` — existing test file, new methods will be added
- `Assets/Tests/EditMode/ECS/AsteroidsEcsTestFixture.cs` — existing base fixture with helpers

---

## Manual-Only Verifications

*All phase behaviors have automated verification.*

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
