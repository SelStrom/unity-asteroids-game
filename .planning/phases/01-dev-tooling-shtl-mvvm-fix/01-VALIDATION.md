---
phase: 1
slug: dev-tooling-shtl-mvvm-fix
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-02
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit (Unity Test Framework 1.1.33) |
| **Config file** | Assets/Tests/EditMode/EditModeTests.asmdef, Assets/Tests/PlayMode/PlayModeTests.asmdef |
| **Quick run command** | Unity Editor > Window > General > Test Runner > EditMode > Run All |
| **Full suite command** | Unity Editor > Window > General > Test Runner > Run All |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run EditMode tests via Unity Test Runner
- **After every plan wave:** Run full suite (EditMode + PlayMode)
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 01-01-01 | 01 | 1 | MVVM-01 | manual | Verify package.json change | N/A | ⬜ pending |
| 01-01-02 | 01 | 1 | MVVM-02 | manual | Verify #if directive in DevWidget.cs | N/A | ⬜ pending |
| 01-01-03 | 01 | 1 | MVVM-03 | unit | EditMode test: TMP types accessible | ❌ W0 | ⬜ pending |
| 01-01-04 | 01 | 1 | MVVM-04 | unit | EditMode test: bindings work | ❌ W0 | ⬜ pending |
| 01-02-01 | 02 | 1 | TOOL-01 | manual | Verify Unity-MCP installed in manifest | N/A | ⬜ pending |
| 01-02-02 | 02 | 1 | TOOL-02 | manual | Verify test asmdef files exist | N/A | ⬜ pending |
| 01-02-03 | 02 | 1 | TST-11 | unit | NUnit test runs green | ❌ W0 | ⬜ pending |
| 01-03-01 | 03 | 2 | MVVM-05 | manual | Git tag exists on shtl-mvvm | N/A | ⬜ pending |
| 01-03-02 | 03 | 2 | MVVM-06 | manual | manifest.json references new tag | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Assets/Tests/EditMode/EditModeTests.asmdef` — EditMode test assembly
- [ ] `Assets/Tests/PlayMode/PlayModeTests.asmdef` — PlayMode test assembly
- [ ] `Assets/Tests/EditMode/ShtlMvvmCompatibilityTests.cs` — stubs for MVVM-03, MVVM-04

*Test framework already installed (com.unity.test-framework 1.1.33).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Unity-MCP interaction with Claude Code | TOOL-01 | Requires running Unity Editor + Claude Code CLI | Open Unity, Window > AI Game Developer, verify MCP server starts |
| shtl-mvvm compiles on Unity 6.3 | MVVM-04 | Requires Unity 6.3 installation (Phase 2) | Deferred to Phase 2 first build |
| Git tag published to GitHub | MVVM-05 | Requires network + GitHub access | `git ls-remote --tags https://github.com/SelStrom/shtl-mvvm.git` |

*Assembly forwarding verification deferred to Phase 2 first build.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
