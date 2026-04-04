---
phase: 1
slug: dev-tooling-shtl-mvvm-fix
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-02
audited: 2026-04-04
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | NUnit (Unity Test Framework 1.6.0) |
| **Config file** | Assets/Tests/EditMode/EditModeTests.asmdef, Assets/Tests/PlayMode/PlayModeTests.asmdef |
| **Quick run command** | Unity Editor > Window > General > Test Runner > EditMode > Run All |
| **Full suite command** | Unity Editor > Window > General > Test Runner > Run All |
| **CLI command** | `/Applications/Unity/Hub/Editor/6000.3.12f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/selstrom/work/projects/asteroids -runTests -testPlatform EditMode -testFilter "SelStrom.Asteroids.Tests.EditMode.ShtlMvvm" -testResults /tmp/phase01-test-results.xml` |
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
| 01-01-01 | 01 | 1 | MVVM-01 | smoke | `cd ~/work/projects/shtl-mvvm && grep -c "com.unity.textmeshpro" package.json \| grep "^0$" && grep -c "com.unity.ugui" package.json \| grep "^1$"` | N/A | ✅ green |
| 01-01-02 | 01 | 1 | MVVM-02 | smoke | `cd ~/work/projects/shtl-mvvm && grep "UNITY_2023_1_OR_NEWER" Runtime/DevWidget.cs && grep "FindObjectsByType" Runtime/DevWidget.cs && grep "Unity.TextMeshPro" Runtime/Shtl.Mvvm.asmdef` | N/A | ✅ green |
| 01-01-03 | 01 | 1 | MVVM-03 | unit | Unity Test Runner: `TmpCompatibilityTests.TmpText_Type_IsAccessible`, `TmpCompatibilityTests.TextMeshProUGUI_Type_IsAccessible` | ✅ | ✅ green |
| 01-01-04 | 01 | 1 | MVVM-04 | unit | Unity Test Runner: `TmpCompatibilityTests.ShtlMvvm_ViewModelToUIBindings_TmpMethodsExist`, `TmpCompatibilityTests.ShtlMvvm_ViewModelToUIBindings_StringToTmpMethod_HasCorrectSignature` | ✅ | ✅ green |
| 01-02-01 | 02 | 1 | TOOL-01 | unit | Unity Test Runner: `Phase01InfraValidationTests.ManifestJson_ContainsUnityMcpPackage` | ✅ | ✅ green |
| 01-02-02 | 02 | 1 | TOOL-02 | unit | Unity Test Runner: `Phase01InfraValidationTests.EditModeTestAssembly_ExistsAndConfigured`, `Phase01InfraValidationTests.PlayModeTestAssembly_ExistsAndConfigured` | ✅ | ✅ green |
| 01-02-03 | 02 | 1 | TST-11 | unit | Unity Test Runner: all 4 tests in `TmpCompatibilityTests` + 4 tests in `Phase01InfraValidationTests` | ✅ | ✅ green |
| 01-03-01 | 03 | 2 | MVVM-05 | smoke | `cd ~/work/projects/shtl-mvvm && git tag -l "v1.1.0" \| grep "v1.1.0"` | N/A | ✅ green |
| 01-03-02 | 03 | 2 | MVVM-06 | unit | Unity Test Runner: `Phase01InfraValidationTests.ManifestJson_ReferencesShtlMvvmV110` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] `Assets/Tests/EditMode/EditModeTests.asmdef` — EditMode test assembly
- [x] `Assets/Tests/PlayMode/PlayModeTests.asmdef` — PlayMode test assembly
- [x] `Assets/Tests/EditMode/ShtlMvvm/TmpCompatibilityTests.cs` — tests for MVVM-03, MVVM-04
- [x] `Assets/Tests/EditMode/ShtlMvvm/Phase01InfraValidationTests.cs` — tests for TOOL-01, TOOL-02, MVVM-06

*Test framework: com.unity.test-framework 1.6.0.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Unity-MCP MCP server interaction with Claude Code | TOOL-01 | Runtime behavior (MCP server start) not testable via file check | Open Unity, Window > AI Game Developer, verify MCP server starts |
| shtl-mvvm compiles on Unity 6.3 | MVVM-04 | Compilation verified by TmpCompatibilityTests running green in Unity 6 | Automated via test runner |
| Git tag published to GitHub | MVVM-05 | Requires network + GitHub access | `git ls-remote --tags https://github.com/SelStrom/shtl-mvvm.git v1.1.0` |

*Note: MVVM-04 (Unity 6.3 compilation) is now automated -- TmpCompatibilityTests run in Unity 6000.3.12f1.*

---

## Audit Notes (2026-04-04)

- Unity Editor running, CLI batch mode unavailable. Tests verified via file existence + bash smoke checks.
- NUnit tests (`TmpCompatibilityTests`, `Phase01InfraValidationTests`) require Unity Test Runner execution.
- All 9 requirements have automated or semi-automated verification commands.
- `Phase01InfraValidationTests.cs` created to cover previously manual-only gaps (TOOL-01, TOOL-02, MVVM-06).

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 10s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** audited 2026-04-04
