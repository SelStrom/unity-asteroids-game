---
phase: 6
slug: legacy-cleanup
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-03
audited: 2026-04-04
---

# Phase 6 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit) 1.1.33 |
| **Config file** | Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef |
| **Quick run command** | `Unity -runTests -testPlatform EditMode -testFilter LegacyCleanupValidation` |
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
| 06-01-T1 | 01 | 1 | LC-03 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC03_ApplicationClass_HasNoUseEcsField` | yes | green |
| 06-01-T2 | 01 | 1 | LC-04 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC04_ActionScheduler_CanBeUsedStandalone` | yes | green |
| 06-02-T1 | 02 | 2 | LC-02 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC02_ModelFactory_IsAbsentFromAssembly` | yes | green |
| 06-02-T1 | 02 | 2 | LC-03 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC03_EntityTypeEnum_ExistsForDispatch` | yes | green |
| 06-02-T2 | 02 | 2 | LC-05 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC05_ScoreData_Singleton_CanStoreAndReadScore` | yes | green |
| 06-03-T1 | 03 | 3 | LC-01 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC01_LegacySystems_AreAbsentFromAssembly` | yes | green |
| 06-03-T1 | 03 | 3 | LC-02 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC02_LegacyModelsAndComponents_AreAbsentFromAssembly` | yes | green |
| 06-03-T2 | 03 | 3 | LC-06 | unit | `Unity -runTests -testPlatform EditMode -testFilter LC06_ObservableBridgeSystem_HasNoModelDependency` | yes | green |
| 06-04-T1 | 04 | 4 | LC-06 | suite | `Unity -runTests -testPlatform EditMode` | yes | green |
| 06-04-T2 | 04 | 4 | LC-07 | manual | N/A (human-verify checkpoint) | N/A | green |

*Status: pending / green / red / flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. ECS test assemblies and fixtures already exist from Phase 4-5.

---

## Test File Created by Audit

| File | Tests | Requirements Covered |
|------|-------|---------------------|
| Assets/Tests/EditMode/ECS/LegacyCleanupValidationTests.cs | 14 | LC-01, LC-02, LC-03, LC-04, LC-05, LC-06 |

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Gameplay 1:1 without legacy layer | LC-07 | Visual/gameplay verification requires running game | Play full game cycle in Editor, verify all mechanics work |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved (Nyquist audit 2026-04-04)
