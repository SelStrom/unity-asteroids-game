---
phase: 2
slug: unity-6-3-upgrade
status: audited
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-02
audited: 2026-04-03
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
| 02-01-01 | 01 | 1 | UPG-03 | unit | TmpIntegrationTests (3 tests) | ✅ | ✅ green |
| 02-01-02 | 01 | 1 | UPG-04 | smoke | GameplaySmokeTests.SceneLoadsSuccessfully | ✅ | ✅ green |
| 02-02-01 | 02 | 2 | UPG-02 | unit (EditMode) | UpgradeValidationTests (3 tests) | ✅ | ✅ green |
| 02-02-02 | 02 | 2 | UPG-01 | unit (EditMode) | PackageCompatibilityTests (4 tests) | ✅ | ✅ green |
| 02-02-03 | 02 | 2 | UPG-05 | PlayMode + manual | GameplaySmokeTests (3 tests) + UAT | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `Assets/Tests/EditMode/Upgrade/UpgradeValidationTests.cs` — deprecated API checks (UPG-02)
- [x] `Assets/Tests/EditMode/Upgrade/TmpIntegrationTests.cs` — TMP type accessibility (UPG-03)
- [x] `Assets/Tests/PlayMode/Upgrade/GameplaySmokeTests.cs` — scene loading + key objects (UPG-05)
- [x] `Assets/Tests/EditMode/Upgrade/PackageCompatibilityTests.cs` — package compatibility (UPG-01)

*All Wave 0 test files created and passing.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Полный цикл геймплея 1:1 | UPG-05 | Полный gameplay требует человека | Play game: ship moves, shoots bullets/laser, asteroids split, UFOs spawn, leaderboard submits |
| Visual TMP rendering | UPG-03 | Рендеринг шрифтов — визуальная проверка | Verify all UI text renders correctly (HUD, score, leaderboard) |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** passed

---

## Validation Audit 2026-04-03

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |
| Tests run | 13 (10 EditMode + 3 PlayMode) |
| All green | Yes |
