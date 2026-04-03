---
phase: 6
slug: legacy-cleanup
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-03
---

# Phase 6 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit) 1.6.0 |
| **Config file** | Assets/Tests/EditMode/ECS/EcsEditModeTests.asmdef |
| **Quick run command** | `Unity -runTests -testPlatform EditMode -testFilter ECS` |
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
| TBD | TBD | TBD | TBD | TBD | TBD | TBD | TBD |

*Will be populated after planning. Status: pending / green / red / flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. ECS test assemblies and fixtures already exist from Phase 4-5.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Gameplay 1:1 without legacy layer | SC-7 | Visual/gameplay verification requires running game | Play full game cycle in Editor, verify all mechanics work |
| No compilation errors after deletion | SC-1,2 | Compilation only in Unity Editor | Open project, check Console for zero errors |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
