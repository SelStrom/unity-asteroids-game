---
phase: 14
slug: config-visual-polish
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-05
---

# Phase 14 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Unity Test Framework 1.1.33 (NUnit) |
| **Config file** | `Assets/Tests/EditMode/EditMode.asmdef` |
| **Quick run command** | `tests-run --testMode EditMode --testFilter Rocket` |
| **Full suite command** | `tests-run --testMode EditMode` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `tests-run --testMode EditMode --testFilter Rocket`
- **After every plan wave:** Run `tests-run --testMode EditMode`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 14-01-01 | 01 | 1 | CONF-01 | — | N/A | unit | `tests-run --testFilter RocketConfig` | ❌ W0 | ⬜ pending |
| 14-01-02 | 01 | 1 | CONF-01 | — | N/A | unit | `tests-run --testFilter RocketAmmo` | ✅ | ⬜ pending |
| 14-02-01 | 02 | 1 | VIS-02 | — | N/A | manual | MCP screenshot-game-view | N/A | ⬜ pending |
| 14-02-02 | 02 | 1 | VIS-04 | — | N/A | manual | MCP screenshot-game-view | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/ECS/RocketConfigTests.cs` — тест что EntityFactory.CreateRocket добавляет ScoreValue
- Существующая инфраструктура покрывает остальные требования (`RocketAmmoSystemTests.cs`, `EntityFactoryTests.cs`)

*Existing infrastructure covers most phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Trail ParticleSystem визуально рендерится за ракетой | VIS-02 | Визуальный эффект, нельзя автоматизировать | Запустить игру, выстрелить ракету (R), проверить наличие следа через MCP screenshot |
| VFX взрыв при уничтожении ракеты | VIS-04 | Визуальный эффект, нельзя автоматизировать | Запустить игру, дождаться попадания ракеты, проверить через MCP screenshot |
| Trail корректно очищается при pool reuse | VIS-02 | Пулинг + визуал | Выстрелить ракету, дождаться уничтожения, выстрелить снова — не должно быть "хвоста" |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
