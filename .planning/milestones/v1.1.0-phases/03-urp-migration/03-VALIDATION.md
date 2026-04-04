---
phase: 3
slug: urp-migration
status: audited
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-02
audited: 2026-04-03
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit) 1.6.0 |
| **Config file** | `Assets/Tests/EditMode/EditModeTests.asmdef`, `Assets/Tests/PlayMode/PlayModeTests.asmdef` |
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

| Task ID | Plan | Wave | Requirement | Test Type | Test Files | Status |
|---------|------|------|-------------|-----------|------------|--------|
| 03-01-01 | 01 | 1 | URP-01, URP-02 | automated | UrpSetupTests.cs (4 tests) | ✅ green |
| 03-01-02 | 01 | 1 | URP-03 | automated | UrpMaterialTests.cs (5 tests) | ✅ green |
| 03-01-03 | 01 | 1 | URP-04 | automated | UrpPostProcessingTests.cs (3 tests) | ✅ green |
| 03-02-01 | 02 | 2 | URP-01..URP-04 | automated | All 12 tests above | ✅ green |
| 03-02-02 | 02 | 2 | URP-05 | manual | Visual comparison in Unity Editor | ✅ manual pass |
| 03-02-03 | 02 | 2 | URP-06 | manual | Full gameplay cycle | ✅ manual pass |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `UrpSetupTests.cs` — 4 EditMode tests for URP pipeline setup (URP-01, URP-02)
- [x] `UrpMaterialTests.cs` — 5 EditMode tests for material conversion (URP-03)
- [x] `UrpPostProcessingTests.cs` — 3 EditMode tests for Post-Processing (URP-04)

Total: 12 automated EditMode tests covering URP-01 through URP-04. All green.

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

---

## Validation Audit 2026-04-03

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |
| Tests run | 12 EditMode |
| All green | Yes |
