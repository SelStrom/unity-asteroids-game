---
phase: 3
slug: urp-migration
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-02
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit) 1.6.0 |
| **Config file** | `Assets/Tests/EditMode/EditMode.asmdef`, `Assets/Tests/PlayMode/PlayMode.asmdef` |
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

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 1 | URP-01 | manual | Verify URP Asset in Project Settings | N/A | ⬜ pending |
| 03-01-02 | 01 | 1 | URP-02 | manual | Check prefabs — no pink materials | N/A | ⬜ pending |
| 03-01-03 | 01 | 1 | URP-03 | manual | Check vfx_blow particle effect renders | N/A | ⬜ pending |
| 03-02-01 | 02 | 1 | URP-04 | manual | Verify Volume with Bloom/Vignette active | N/A | ⬜ pending |
| 03-02-02 | 02 | 1 | URP-05 | manual | Visual comparison with original | N/A | ⬜ pending |
| 03-02-03 | 02 | 1 | URP-06 | integration | Run game in Editor, full gameplay cycle | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. URP migration is primarily asset/config work — automated unit tests have limited applicability for render pipeline changes.

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

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
