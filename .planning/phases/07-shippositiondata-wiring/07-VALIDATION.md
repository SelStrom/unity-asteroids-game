---
phase: 7
slug: shippositiondata-wiring
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-03
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
| 07-01-01 | 01 | 1 | ECS-09, ECS-10 | unit | Test Runner -> SingletonInitTests | ❌ W0 | ⬜ pending |
| 07-01-02 | 01 | 1 | ECS-09, ECS-10 | unit | Test Runner -> ShootToSystemTests, MoveToSystemTests | ✅ | ⬜ pending |
| 07-02-01 | 02 | 1 | LC-01..LC-07 | documentation | Manual review | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/ECS/SingletonInitTests.cs` — regression test for ShipPositionData singleton creation in InitializeEcsSingletons()

*Existing ShootTo/MoveTo system tests cover logic but use test fixture that masks production wiring gap.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Full gameplay 1:1 with UFO AI | LC-07 | Requires visual confirmation of UFO shooting and pursuing player | 1. Start game in Editor 2. Wait for UFO spawn 3. Verify small UFO pursues player 4. Verify UFO shoots at player with lead targeting |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
