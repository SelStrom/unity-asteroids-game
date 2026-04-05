---
phase: 12
slug: bridge-lifecycle
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-05
---

# Phase 12 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.1.33 (NUnit) |
| **Config file** | `Assets/Tests/EditMode/EditMode.asmdef` |
| **Quick run command** | `unity -runTests -testPlatform EditMode -testFilter RocketLifecycle` |
| **Full suite command** | `unity -runTests -testPlatform EditMode` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run EditMode tests filtered by Rocket
- **After every plan wave:** Run full EditMode suite
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 12-01-T1 | 01 | 1 | VIS-03 | — | N/A | unit | `GameObjectSyncSystemTests` | ❌ W0 | ⬜ pending |
| 12-01-T2 | 01 | 1 | D-07 | — | N/A | grep | `grep RocketShootEvent RocketShootEvent.cs` | ❌ W0 | ⬜ pending |
| 12-02-T1 | 02 | 2 | VIS-01 | — | N/A | grep | `grep RocketVisual RocketVisual.cs` | ❌ W0 | ⬜ pending |
| 12-02-T2 | 02 | 2 | D-08, D-09 | — | N/A | grep | `grep ProcessRocketEvents ShootEventProcessorSystem.cs` | ❌ W0 | ⬜ pending |
| 12-03-T1 | 03 | 3 | TEST-02 | — | N/A | integration | `RocketLifecycleTests` | ❌ W0 | ⬜ pending |
| 12-03-T2 | 03 | 3 | VIS-01 | — | N/A | checkpoint:human-verify | — | — | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/ECS/RocketLifecycleTests.cs` — stubs for TEST-02
- [ ] Тест синхронизации rotation для RocketTag в `GameObjectSyncSystemTests.cs` — covers VIS-03

*Existing infrastructure covers ECS test patterns. New test files needed for rocket-specific validation.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Ракета отображается как уменьшенный спрайт корабля | VIS-01 | Визуальная проверка в Unity Editor | 1. Enter Play mode 2. Launch rocket 3. Verify sprite is smaller ship sprite |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
