---
phase: 13
slug: input-game-integration
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-05
---

# Phase 13 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework (NUnit) 1.1.33 |
| **Config file** | `Assets/Tests/EditMode/EditMode.asmdef` |
| **Quick run command** | `mcp__ai-game-developer__tests-run --testMode EditMode --testFilter RocketAmmo` |
| **Full suite command** | `mcp__ai-game-developer__tests-run --testMode EditMode` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick run command (RocketAmmo filter)
- **After every plan wave:** Run full EditMode suite
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 13-01-01 | 01 | 1 | ROCK-01 | — | N/A | unit | `tests-run --testFilter RocketAmmoShoot` | ❌ W0 | ⬜ pending |
| 13-01-02 | 01 | 1 | ROCK-01 | — | N/A | unit | `tests-run --testFilter RocketAmmoEmpty` | ❌ W0 | ⬜ pending |
| 13-02-01 | 02 | 1 | ROCK-01 | — | N/A | integration | MCP visual verify | ❌ manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/ECS/RocketAmmoSystemTests.cs` — extend with shooting logic tests (stubs exist from Phase 10)
- [ ] Existing test infrastructure covers all framework needs

*If none: "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| R key launches rocket in-game | ROCK-01 | Input System integration cannot be unit tested | Enter Play Mode, press R, verify rocket spawns from ship position |
| Rocket not launched on empty ammo | ROCK-01 | Requires game state with depleted ammo | Fire all rockets, press R again, verify no rocket spawns |
| Restart clears rockets and resets ammo | ROCK-01 | Full game cycle | Launch rockets, restart game, verify rockets cleared and ammo reset |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
