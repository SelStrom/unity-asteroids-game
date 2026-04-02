---
phase: 3
slug: urp-migration
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-02
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit) 1.6.0 |
| **Config file** | `Assets/Tests/EditMode/EditModeTests.asmdef`, `Assets/Tests/PlayMode/PlayMode.asmdef` |
| **Quick run command** | Unity Editor > Window > General > Test Runner > EditMode > Run All |
| **Full suite command** | Unity Editor > Window > General > Test Runner > All > Run All |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Визуальная проверка в Unity Editor (нет розовых материалов)
- **After every plan wave:** Полный запуск EditMode + PlayMode тестов
- **Before `/gsd:verify-work`:** Full suite must be green + визуальная верификация
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | Test Files | Status |
|---------|------|------|-------------|-----------|-------------------|------------|--------|
| 03-01-01 | 01 | 1 | URP-01, URP-02 | automated + manual | `grep -q "com.unity.render-pipelines.universal" Packages/manifest.json && test -f Assets/Settings/URP-2D-Asset.asset && test -f Assets/Settings/URP-2D-Renderer.asset` | UrpSetupTests.cs (4 tests) | pending |
| 03-01-02 | 01 | 1 | URP-03 | automated | `test -f Assets/Media/materials/Laser-URP.mat && test -f Assets/Media/materials/Particle-URP.mat` | UrpMaterialTests.cs (5 tests) | pending |
| 03-01-03 | 01 | 1 | URP-04 | automated | `test -f Assets/Settings/PostProcessing-Profile.asset` | UrpPostProcessingTests.cs (3 tests) | pending |
| 03-02-01 | 02 | 2 | URP-01..URP-04 | automated | `grep -q "UrpSetupTests" Assets/Tests/EditMode/Upgrade/UrpSetupTests.cs && grep -q "UrpMaterialTests" Assets/Tests/EditMode/Upgrade/UrpMaterialTests.cs && grep -q "UrpPostProcessingTests" Assets/Tests/EditMode/Upgrade/UrpPostProcessingTests.cs` | UrpSetupTests, UrpMaterialTests, UrpPostProcessingTests (12 tests total) | pending |
| 03-02-02 | 02 | 2 | URP-05 | manual | Visual comparison in Unity Editor Game View | N/A (human-verify checkpoint) | pending |
| 03-02-03 | 02 | 2 | URP-06 | manual | Full gameplay cycle in Unity Editor | N/A (human-verify checkpoint) | pending |

*Status: pending / green / red / flaky*

---

## Wave 0 Requirements

Plan 01 Task 2 creates all Wave 0 test infrastructure:
- `UrpSetupTests.cs` — 4 EditMode tests for URP pipeline setup (URP-01, URP-02)
- `UrpMaterialTests.cs` — 5 EditMode tests for material conversion (URP-03)
- `UrpPostProcessingTests.cs` — 3 EditMode tests for Post-Processing (URP-04)

Total: 12 automated EditMode tests covering URP-01 through URP-04. Tests are created alongside the URP configuration in Plan 01, ensuring immediate feedback.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| No pink materials | URP-02, URP-05 | Visual rendering not testable via NUnit | Open each prefab in Scene view, verify correct rendering |
| Particle effects | URP-03 | ParticleSystem visual output requires Editor | Play vfx_blow, verify explosion renders correctly |
| Post-Processing | URP-04 | Volume effects visible only in Game view | Enter Play mode, verify Bloom glow on sprites |
| Gameplay 1:1 | URP-06 | Full integration requires manual play | Play through: ship controls, shooting, asteroids, UFO, scoring |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 15s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved
