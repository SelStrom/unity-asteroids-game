---
phase: 2
slug: unity-6-3-upgrade
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-02
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.6.0 (NUnit) |
| **Config file** | `Assets/Tests/EditMode/EditModeTests.asmdef`, `Assets/Tests/PlayMode/PlayModeTests.asmdef` |
| **Quick run command** | Unity Editor > Window > General > Test Runner > EditMode > Run All |
| **Full suite command** | Unity Editor > Window > General > Test Runner > All > Run All |
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
| 02-01-01 | 01 | 1 | UPG-03 | unit | Test Runner > EditMode > TmpCompatibilityTests | ✅ (Phase 1) | ⬜ pending |
| 02-01-02 | 01 | 1 | UPG-04 | smoke | Project opens + 0 errors in Console | manual | ⬜ pending |
| 02-02-01 | 02 | 2 | UPG-02 | unit (EditMode) | Test Runner > EditMode > UpgradeValidationTests | ❌ W0 | ⬜ pending |
| 02-02-02 | 02 | 2 | UPG-01 | smoke | Compilation 0 errors | manual | ⬜ pending |
| 02-02-03 | 02 | 2 | UPG-05 | PlayMode + manual | Test Runner > PlayMode > GameplaySmokeTests + UAT | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/Upgrade/UpgradeValidationTests.cs` — stubs for UPG-02
- [ ] `Assets/Tests/EditMode/Upgrade/TmpIntegrationTests.cs` — extends UPG-03
- [ ] `Assets/Tests/PlayMode/Upgrade/GameplaySmokeTests.cs` — covers UPG-05

*Existing TMP compatibility tests from Phase 1 cover partial UPG-03.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Проект открывается без ошибок | UPG-01 | Requires Unity Editor open | Open project in Unity 6.3, check Console for 0 errors |
| Геймплей 1:1 | UPG-05 | Full gameplay requires human verification | Play game: ship moves, shoots bullets/laser, asteroids split, UFOs spawn, leaderboard submits |
| Visual TMP rendering | UPG-03 | Font rendering requires visual check | Verify all UI text renders correctly (HUD, score, leaderboard) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
