---
phase: 7
slug: shippositiondata-wiring
status: audited
nyquist_compliant: false
wave_0_complete: true
created: 2026-04-03
audited: 2026-04-04
---

# Phase 7 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.1.33 (NUnit) |
| **Config file** | Assets/Tests/EditMode/EditMode.asmdef, Assets/Tests/PlayMode/PlayMode.asmdef |
| **Quick run command** | Unity Editor -> Window -> General -> Test Runner -> EditMode -> Run All |
| **Full suite command** | Unity Editor -> Test Runner -> Run All (EditMode + PlayMode) |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run EditMode tests via Test Runner
- **After every plan wave:** Run full suite (EditMode + PlayMode)
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 07-01-01 | 01 | 1 | ECS-09, ECS-10 | unit | Test Runner -> SingletonInitTests | ✅ | ⬜ needs-run |
| 07-01-02 | 01 | 1 | ECS-09, ECS-10 | unit | Test Runner -> ShootToSystemTests, MoveToSystemTests | ✅ | ⬜ needs-run |
| 07-02-01 | 02 | 1 | LC-01..LC-07 | documentation | Manual review | N/A | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `Assets/Tests/EditMode/ECS/SingletonInitTests.cs` — regression test for ShipPositionData singleton creation in InitializeEcsSingletons()

*Existing ShootTo/MoveTo system tests cover logic but use test fixture that masks production wiring gap.*
*Wave 0 file created during Phase 7 Plan 01 execution (commit ca21cae).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Full gameplay 1:1 with UFO AI | LC-07 | Requires visual confirmation of UFO shooting and pursuing player | 1. Start game in Editor 2. Wait for UFO spawn 3. Verify small UFO pursues player 4. Verify UFO shoots at player with lead targeting |

---

## Audit Notes (2026-04-04)

- **07-01-01**: SingletonInitTests.cs exists (3 tests: creation, default values, idempotency). Structurally correct, mirrors idempotent pattern from Application.InitializeEcsSingletons(). Cannot run from CLI -- Unity Editor holds project lock. Requires Test Runner execution.
- **07-01-02**: ShootToSystemTests.cs (3 tests) and MoveToSystemTests.cs (3 tests) exist. Both use AsteroidsEcsTestFixture with CreateShipPositionSingleton helper. Cannot run from CLI -- same reason.
- **07-02-01**: Traceability table verified. REQUIREMENTS.md contains all 9 requirements (ECS-09, ECS-10: Phase 7 Complete; LC-01..LC-06: Phase 6 Complete; LC-07: Phase 7 Complete). UAT approved per 07-02-SUMMARY.md.

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter (blocked: test execution requires Unity Test Runner)

**Approval:** partial -- documentation gap (07-02-01) resolved, test files exist but execution blocked by Unity Editor lock. Run in Test Runner to complete.
